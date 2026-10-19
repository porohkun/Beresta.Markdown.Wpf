namespace Beresta.Markdown.Wpf.Tests;

using System.IO;
using System.Windows.Media.Imaging;
using Presenters;

/// <summary>Общее состояние картинок: сброс кэша загружает показанные картинки заново, предел загрузчика отсекает большие.</summary>
[Collection(SharedImageState.Name)]
public sealed class ImageCacheTests
{
    [Fact]
    public void Сброшенная_из_кэша_картинка_поменявшегося_файла_загружается_заново()
    {
        var path = Path.Combine(Path.GetTempPath(), $"md-image-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, ImageTests.Png(10, 4));
        try
        {
            WpfHost.Invoke(() =>
            {
                var viewer = new MarkdownViewer { Markdown = $"![picture]({new Uri(path).AbsoluteUri})" };
                var window = WpfHost.Show(viewer);
                ImageTests.WaitFor(window, () => WidthOf(viewer) == 10);

                File.WriteAllBytes(path, ImageTests.Png(20, 4));
                MdImageCache.Invalidate(new Uri(path));
                ImageTests.WaitFor(window, () => WidthOf(viewer) == 20);

                WidthOf(viewer).Should().Be(20);
                window.Close();
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Очищенный_кэш_загружает_все_картинки_заново()
    {
        WpfHost.Invoke(() =>
        {
            var loader = new ImageTests.CountingLoader();
            var viewer = new MarkdownViewer { ImageLoader = loader, Markdown = "![a](cleared-a.png) ![b](cleared-b.png)" };
            var window = WpfHost.Show(viewer);
            ImageTests.WaitFor(window, () => loader.Calls == 2);

            MdImageCache.Clear();
            ImageTests.WaitFor(window, () => loader.Calls == 4);

            loader.Calls.Should().Be(4);
            window.Close();
        });
    }

    [Fact]
    public async Task Картинка_больше_предела_загрузки_не_загружается()
    {
        var data = "data:image/png;base64," + Convert.ToBase64String(ImageTests.Png(100, 100));
        var limit = MdImageLoader.MaxDownloadBytes;
        MdImageLoader.MaxDownloadBytes = 16;
        try
        {
            (await MdImageLoader.Instance.LoadAsync(data, null, 0, CancellationToken.None)).Should().BeNull();
        }
        finally
        {
            MdImageLoader.MaxDownloadBytes = limit;
        }

        (await MdImageLoader.Instance.LoadAsync(data, null, 0, CancellationToken.None)).Should().NotBeNull();
    }

    /// <summary>Ширина показанной картинки вьюера в пикселях; нет её — 0.</summary>
    private static int WidthOf(MarkdownViewer viewer)
        => Visuals.Descendants<MdImage>(viewer).SingleOrDefault()?.ImageSource is BitmapSource bitmap ? bitmap.PixelWidth : 0;
}
