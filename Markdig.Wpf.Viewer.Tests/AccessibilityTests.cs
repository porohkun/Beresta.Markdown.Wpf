namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Presenters;

/// <summary>Дерево автоматизации и клавиатура.</summary>
public sealed class AccessibilityTests
{
    [Fact]
    public void Вьюер_для_автоматизации_документ_без_строк_вьюмоделей()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer
            {
                Markdown = "# Title\n\nfirst paragraph\n\n- item one\n- item two\n\n> quoted\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n```\ncode\n```"
            };
            var window = WpfHost.Show(viewer, height: 700);

            var peer = UIElementAutomationPeer.CreatePeerForElement(viewer);
            var names = Descendants(peer).Select(child => child.GetName()).ToList();

            peer.GetAutomationControlType().Should().Be(AutomationControlType.Document);
            names.Should().Contain(["Title", "first paragraph", "item one", "quoted", "code"]);
            names.Should().NotContain(name => name.Contains("Vm") || name.Contains("ViewModels"));
            window.Close();
        });
    }

    [Fact]
    public void Заголовок_знает_свой_уровень()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "## Second level" };
            var window = WpfHost.Show(viewer);

            var heading = Visuals.Descendants<MdHeadingBlock>(viewer).Single();
            UIElementAutomationPeer.CreatePeerForElement(heading).GetHeadingLevel().Should().Be(AutomationHeadingLevel.Level2);
            window.Close();
        });
    }

    [Fact]
    public void Картинка_для_автоматизации_картинка_со_своим_alt()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { ImageLoader = new OnePixel(), Markdown = "![company logo](logo.png)" };
            var window = WpfHost.Show(viewer);

            var peer = UIElementAutomationPeer.CreatePeerForElement(Visuals.Descendants<MdImage>(viewer).Single());
            peer.GetAutomationControlType().Should().Be(AutomationControlType.Image);
            peer.GetName().Should().Be("company logo");
            window.Close();
        });
    }

    [Fact]
    public void Ссылку_сноски_нажимают_через_автоматизацию()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "text[^1]\n\n[^1]: note" };
            var navigated = new List<string>();
            MarkdownLink.AddNavigateHandler(viewer, (_, e) => navigated.Add(e.Href));
            var window = WpfHost.Show(viewer);
            var link = Visuals.Descendants<MdFootnoteLink>(viewer).First(footnote => !footnote.Link!.IsBack);

            var peer = UIElementAutomationPeer.CreatePeerForElement(link);
            peer.GetAutomationControlType().Should().Be(AutomationControlType.Hyperlink);
            ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();

            navigated.Should().Equal("#fn:1");
            window.Close();
        });
    }

    [Fact]
    public void Ссылка_берёт_фокус_с_клавиатуры_и_открывается_по_Enter()
    {
        WpfHost.Invoke(() =>
        {
            var opened = new List<string>();
            var viewer = new MarkdownViewer { Markdown = "see [the site](https://a.example)" };
            MarkdownLink.SetOpener(viewer, new Recording(opened));
            var window = WpfHost.Show(viewer);
            var link = Visuals.Descendants<MdTextBlock>(viewer).Single().Inlines.OfType<Hyperlink>().Single();

            link.Focusable.Should().BeTrue();
            KeyboardNavigation.GetIsTabStop(link).Should().BeTrue();
            link.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(viewer)!, 0, Key.Enter)
            {
                RoutedEvent = Keyboard.KeyDownEvent
            });

            opened.Should().Equal("https://a.example");
            window.Close();
        });
    }

    private static IEnumerable<AutomationPeer> Descendants(AutomationPeer peer)
        => (peer.GetChildren() ?? []).SelectMany(child => Descendants(child).Prepend(child));

    private sealed class OnePixel : IMdImageLoader
    {
        public Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken)
        {
            var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[4], 4);
            bitmap.Freeze();
            return Task.FromResult<ImageSource?>(bitmap);
        }
    }

    private sealed class Recording(List<string> opened) : IMdLinkOpener
    {
        public void Open(string href, Uri? uri) => opened.Add(href);
    }
}
