namespace Beresta.Markdown.Wpf.Tests;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Presenters;

/// <summary>Загрузка картинок: повтор после неудачи, размер декодирования, кэш по памяти, отмена.</summary>
public sealed class ImageTests
{
    [Fact]
    public void Неудачная_загрузка_не_запоминается()
    {
        WpfHost.Invoke(() =>
        {
            var loader = new FlakyLoader();
            var first = new MarkdownViewer { ImageLoader = loader, Markdown = "![a](later.png)" };
            var window = WpfHost.Show(first);
            Visuals.Descendants<MdImage>(first).Single().ImageSource.Should().BeNull();

            var second = new MarkdownViewer { ImageLoader = loader, Markdown = "![a](later.png)" };
            window.Content = second;
            WpfHost.Settle(window);

            loader.Calls.Should().Be(2);
            Visuals.Descendants<MdImage>(second).Single().ImageSource.Should().NotBeNull();
            window.Close();
        });
    }

    [Fact]
    public void Большая_картинка_декодируется_не_шире_экрана_а_с_шириной_из_разметки_по_ней()
    {
        WpfHost.Invoke(() =>
        {
            var data = "data:image/png;base64," + Convert.ToBase64String(Png(6000, 4));
            var viewer = new MarkdownViewer { Markdown = $"![wide]({data})\n\n![small]({data}){{width=100}}" };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });
            WaitFor(window, () => Visuals.Descendants<MdImage>(viewer).All(image => image.ImageSource is not null));

            var images = Visuals.Descendants<MdImage>(viewer).Select(image => (BitmapSource)image.ImageSource!).ToList();
            var screen = SystemParameters.PrimaryScreenWidth * VisualTreeHelper.GetDpi(viewer).DpiScaleX;
            images[0].PixelWidth.Should().BeLessThan(6000).And.BeGreaterThanOrEqualTo((int)screen);
            images[1].PixelWidth.Should().BeLessThanOrEqualTo(256);
            window.Close();
        });
    }

    [Fact]
    public void Кэш_вытесняет_давние_картинки_сверх_бюджета_памяти()
    {
        var cache = new MdImageCache(budget: 3 * 1000 * 4);
        var loads = new List<string>();
        Task<ImageSource?> Load(string key)
        {
            loads.Add(key);
            return Task.FromResult<ImageSource?>(Bitmap(1000, 1));
        }

        foreach (var key in new[] { "a", "b", "c", "d" })
        {
            using var ticket = cache.Request(Loader.Instance, key, 0, _ => Load(key));
        }

        using (cache.Request(Loader.Instance, "d", 0, _ => Load("d")))
        {
        }

        using (cache.Request(Loader.Instance, "a", 0, _ => Load("a")))
        {
        }

        loads.Should().Equal("a", "b", "c", "d", "a");
    }

    [Fact]
    public void Загрузка_без_ждущих_отменяется_и_не_запоминается()
    {
        var cache = new MdImageCache(budget: 1_000_000);
        var tokens = new List<CancellationToken>();
        Task<ImageSource?> Load(CancellationToken token)
        {
            tokens.Add(token);
            return new TaskCompletionSource<ImageSource?>().Task;
        }

        var first = cache.Request(Loader.Instance, "slow", 0, Load);
        var second = cache.Request(Loader.Instance, "slow", 0, Load);
        tokens.Should().ContainSingle();

        first.Dispose();
        tokens[0].IsCancellationRequested.Should().BeFalse();

        second.Dispose();
        tokens[0].IsCancellationRequested.Should().BeTrue();

        using (cache.Request(Loader.Instance, "slow", 0, Load))
        {
            tokens.Should().HaveCount(2);
        }
    }

    [Fact]
    public void Картинка_ушедшая_из_окна_отменяет_свою_загрузку()
    {
        WpfHost.Invoke(() =>
        {
            var loader = new EndlessLoader();
            var viewer = new MarkdownViewer { ImageLoader = loader, Markdown = "![a](slow.png)" };
            var window = WpfHost.Show(viewer);
            loader.Tokens.Should().ContainSingle().Which.IsCancellationRequested.Should().BeFalse();

            window.Content = null;
            WpfHost.Settle(window);

            loader.Tokens[0].IsCancellationRequested.Should().BeTrue();
            window.Close();
        });
    }

    /// <summary>Прогоняет очередь окна, пока условие не выполнится; фоновая работа успевает за это время.</summary>
    private static void WaitFor(Window window, Func<bool> condition)
    {
        for (var attempt = 0; attempt < 200 && !condition(); attempt++)
        {
            Thread.Sleep(10);
            WpfHost.Settle(window);
        }
    }

    private static BitmapSource Bitmap(int width, int height)
    {
        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, new byte[width * height * 4], width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static byte[] Png(int width, int height)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(Bitmap(width, height)));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    /// <summary>Первый раз картинки ещё нет, потом она есть.</summary>
    private sealed class FlakyLoader : IMdImageLoader
    {
        public int Calls { get; private set; }

        public Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken)
            => Task.FromResult<ImageSource?>(++Calls == 1 ? null : Bitmap(10, 10));
    }

    /// <summary>Загрузка, которая кончается только отменой.</summary>
    private sealed class EndlessLoader : IMdImageLoader
    {
        public List<CancellationToken> Tokens { get; } = [];

        public Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            return Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith(_ => (ImageSource?)null, TaskScheduler.Default);
        }
    }

    /// <summary>Загрузчик-ключ для кэша, сам ничего не грузит.</summary>
    private sealed class Loader : IMdImageLoader
    {
        public static Loader Instance { get; } = new();

        public Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken)
            => Task.FromResult<ImageSource?>(null);
    }
}
