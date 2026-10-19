namespace Beresta.Markdown.Wpf.Tests;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Presenters;

/// <summary>Дерево автоматизации и клавиатура.</summary>
public sealed class AccessibilityTests
{
    private const int WmGetObject = 0x003D;
    private const int UiaRootObjectId = -25;

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

    [Fact]
    public void Узел_документа_знает_все_блоки_и_создаёт_несозданный()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = string.Join("\n\n", Enumerable.Range(0, 200).Select(i => $"Paragraph {i}")) };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer }, height: 200);
            var children = UIElementAutomationPeer.CreatePeerForElement(viewer).GetChildren();
            var last = children[^1];

            children.Should().HaveCount(viewer.Items.Count);
            viewer.BlockPanel!.RealizedRange!.Value.Last.Should().BeLessThan(viewer.Items.Count - 1);
            last.GetName().Should().Be("Paragraph 199");

            ((IVirtualizedItemProvider)last.GetPattern(PatternInterface.VirtualizedItem)).Realize();

            viewer.BlockPanel.RealizedRange!.Value.Last.Should().Be(viewer.Items.Count - 1);
            window.Close();
        });
    }

    [Fact]
    public void Таблица_для_автоматизации_сетка_с_заголовками_столбцов()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "| Name | Size |\n|---|---|\n| alpha | 1 |\n| beta | 2 |" };
            var window = WpfHost.Show(viewer);
            Connect(window);
            var table = UIElementAutomationPeer.CreatePeerForElement(Visuals.Descendants<MdTablePresenter>(viewer).Single());
            var grid = (IGridProvider)table.GetPattern(PatternInterface.Grid);
            var cell = grid.GetItem(1, 1);
            var place = (IGridItemProvider)cell.GetPatternProvider(GridItemPatternIdentifiers.Pattern.Id);

            table.GetAutomationControlType().Should().Be(AutomationControlType.Table);
            (grid.RowCount, grid.ColumnCount).Should().Be((3, 2));
            NameOf(cell).Should().Be("1");
            (place.Row, place.Column).Should().Be((1, 1));
            ((ITableProvider)table.GetPattern(PatternInterface.Table)).GetColumnHeaders().Select(NameOf).Should().Equal("Name", "Size");
            window.Close();
        });
    }

    private static IEnumerable<AutomationPeer> Descendants(AutomationPeer peer)
        => (peer.GetChildren() ?? []).SelectMany(child => Descendants(child).Prepend(child));

    /// <summary>Подключает дерево автоматизации окна, как клиент UI Automation: запросом корня и обходом.</summary>
    private static void Connect(Window window)
    {
        SendMessage(new WindowInteropHelper(window).Handle, WmGetObject, 0, UiaRootObjectId);
        _ = Descendants(UIElementAutomationPeer.CreatePeerForElement(window)).Count();
    }

    private static string NameOf(IRawElementProviderSimple provider)
        => (string)provider.GetPropertyValue(AutomationElementIdentifiers.NameProperty.Id);

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);

    private sealed class OnePixel : IMdImageLoader
    {
        public Task<ImageSource?> LoadAsync(string url, Uri? uri, int decodeWidth, CancellationToken cancellationToken)
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