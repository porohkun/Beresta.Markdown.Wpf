namespace Beresta.Markdown.Wpf.Tests;

using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Presenters;

/// <summary>Политика загрузки картинок: схемы, хосты, сетевые пути к файлам, готовые настройки.</summary>
public sealed class ImagePolicyTests
{
    [Theory]
    [InlineData("https://example.com/a.png", true)]
    [InlineData("file:///C:/docs/a.png", true)]
    [InlineData("data:image/png;base64,AAAA", true)]
    [InlineData("file://server/share/a.png", false)]
    [InlineData(@"\\server\share\a.png", false)]
    public void По_умолчанию_запрещены_только_файлы_на_других_компьютерах(string url, bool allowed)
    {
        MdImagePolicy.Default.Allows(url, Absolute(url)).Should().Be(allowed);
    }

    [Theory]
    [InlineData("https://example.com/a.png", false)]
    [InlineData("http://example.com/a.png", false)]
    [InlineData("file:///C:/docs/a.png", false)]
    [InlineData("data:image/png;base64,AAAA", true)]
    [InlineData("pack://application:,,,/Images/a.png", true)]
    [InlineData("a.png", false)]
    public void Для_чужого_текста_только_картинки_внутри_документа_и_приложения(string url, bool allowed)
    {
        MdImagePolicy.Untrusted.Allows(url, Absolute(url)).Should().Be(allowed);
    }

    [Theory]
    [InlineData("https://example.com/a.png", false)]
    [InlineData("file:///C:/docs/a.png", true)]
    [InlineData("file://server/share/a.png", false)]
    public void Без_сети_файлы_только_свои(string url, bool allowed)
    {
        MdImagePolicy.Offline.Allows(url, Absolute(url)).Should().Be(allowed);
    }

    [Theory]
    [InlineData("https://cdn.example.com/a.png", true)]
    [InlineData("https://CDN.example.com/a.png", true)]
    [InlineData("https://evil.example.org/a.png?data=secret", false)]
    public void Хосты_ограничивают_адреса_веба(string url, bool allowed)
    {
        var policy = new MdImagePolicy { Hosts = ["cdn.example.com"] };

        policy.Allows(url, Absolute(url)).Should().Be(allowed);
    }

    [Fact]
    public void Сетевые_файлы_разрешаются_явно()
    {
        var url = @"\\server\share\a.png";

        new MdImagePolicy { AllowNetworkFiles = true }.Allows(url, Absolute(url)).Should().BeTrue();
    }

    [Fact]
    public void Запрещённую_картинку_загрузчик_не_получает_а_на_её_месте_alt()
    {
        WpfHost.Invoke(() =>
        {
            var loader = new RecordingLoader();
            var viewer = new MarkdownViewer
            {
                ImageLoader = loader,
                ImagePolicy = MdImagePolicy.Untrusted,
                Markdown = "![tracker](https://evil.example/pixel.png?user=me) ![inline](data:image/png;base64,AAAA)"
            };
            var window = WpfHost.Show(viewer);

            loader.Urls.Should().Equal("data:image/png;base64,AAAA");
            var tracker = Visuals.Descendants<MdImage>(viewer).First();
            tracker.ImageSource.Should().BeNull();
            ((TextBlock)tracker.Template.FindName("Alt", tracker)).Text.Should().Be("tracker");
            window.Close();
        });
    }

    [Fact]
    public void Политика_с_окна_доходит_до_вьюеров_внутри()
    {
        WpfHost.Invoke(() =>
        {
            var loader = new RecordingLoader();
            var viewer = new MarkdownViewer { ImageLoader = loader, Markdown = "![a](https://example.com/a.png)" };
            var panel = new StackPanel { Children = { viewer } };
            MarkdownViewer.SetImagePolicy(panel, MdImagePolicy.Untrusted);
            var window = WpfHost.Show(panel);

            loader.Urls.Should().BeEmpty();
            window.Close();
        });
    }

    private static Uri? Absolute(string url)
        => url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ? null
            : Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri
            : null;

    private sealed class RecordingLoader : IMdImageLoader
    {
        public List<string> Urls { get; } = [];

        public Task<ImageSource?> LoadAsync(string url, Uri? uri, int decodeWidth, CancellationToken cancellationToken)
        {
            Urls.Add(url);
            var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[4], 4);
            bitmap.Freeze();
            return Task.FromResult<ImageSource?>(bitmap);
        }
    }
}
