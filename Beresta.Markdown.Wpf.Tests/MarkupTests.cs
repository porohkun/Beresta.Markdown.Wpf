namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Extensions;
using Markdig.Extensions.Footnotes;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Presenters;
using Selection;
using ViewModels;

/// <summary>Картинки, задачи, выравнивание таблиц, сноски и строчный HTML.</summary>
public sealed class MarkupTests
{
    [Fact]
    public void Картинка_становится_виджетом_с_адресом_alt_заголовком_и_размером()
    {
        var image = Inlines("![logo](img/a.png \"Title\"){width=120}").OfType<ImageVm>().Single();

        image.Should().BeEquivalentTo(new { Url = "img/a.png", Alt = "logo", Title = "Title", Width = 120.0, Height = (double?)null });
    }

    [Fact]
    public void Картинка_в_ссылке_несёт_ссылку()
    {
        Inlines("[![badge](b.png)](https://e.com)").OfType<ImageVm>().Single().Href.Should().Be("https://e.com");
    }

    [Fact]
    public void HTML_картинка_тоже_картинка()
    {
        var image = Inlines("see <img src=\"a.png\" alt=\"A\" width=\"50px\"> here").OfType<ImageVm>().Single();

        image.Should().BeEquivalentTo(new { Url = "a.png", Alt = "A", Width = 50.0 });
    }

    [Fact]
    public void Строчный_HTML_даёт_начертания()
    {
        var runs = Inlines("<b>b</b> <i>i</i> <s>s</s> <code>c</code> <kbd>k</kbd> H<sub>2</sub>O x<sup>3</sup>")
            .OfType<RunVm>()
            .Where(run => !string.IsNullOrWhiteSpace(run.Text))
            .ToList();

        runs.Single(run => run.Text == "b").Bold.Should().BeTrue();
        runs.Single(run => run.Text == "i").Italic.Should().BeTrue();
        runs.Single(run => run.Text == "s").Strike.Should().BeTrue();
        runs.Single(run => run.Text == "c").Code.Should().BeTrue();
        runs.Single(run => run.Text == "k").Kbd.Should().BeTrue();
        runs.Single(run => run.Text == "2").Subscript.Should().BeTrue();
        runs.Single(run => run.Text == "3").Superscript.Should().BeTrue();
        runs.Where(run => run.Text.IndexOfAny(['H', 'O', 'x']) >= 0)
            .Should().NotBeEmpty().And.OnlyContain(run => !run.Subscript && !run.Superscript);
    }

    [Fact]
    public void Ссылка_a_href_становится_ссылкой()
    {
        var run = Inlines("<a href=\"https://e.com\" title=\"T\">x</a>").OfType<RunVm>().Single();

        run.Should().BeEquivalentTo(new { Text = "x", Href = "https://e.com", LinkTitle = "T" });
    }

    [Fact]
    public void Перенос_br_становится_жёстким_а_комментарии_скрыты()
    {
        ModelDump.Of("a<br>b <!-- hidden --> c").Should().Be("P(a\nb  c)");
        Inlines("a<br/>b").OfType<LineBreakVm>().Single().IsHard.Should().BeTrue();
        ModelDump.Of("a\n\n<!-- note -->\n\nb").Should().Be("P(a) _ _ P(b)");
    }

    [Fact]
    public void Выравнивание_столбцов_приходит_из_разделителя()
    {
        var table = ModelBuilder.Parse("| a | b | c | d |\n|:--|:-:|--:|---|\n| 1 | 2 | 3 | 4 |").OfType<TableVm>().Single();

        table.Alignments.Should().Equal(TableAlignment.Left, TableAlignment.Center, TableAlignment.Right, TableAlignment.Default);
    }

    [Fact]
    public void Сноска_стоит_на_своём_месте_и_даёт_якоря()
    {
        var blocks = ModelBuilder.Parse("text[^n]\n\n[^n]: note\n\nafter");
        var map = MdTextMap.Build(blocks);

        ModelDump.Of(blocks).Should().Be("P(text^1) _ F1[P(note↩)] _ P(after)");
        map.TryGetAnchor("fn:1", out var definition).Should().BeTrue();
        map.TryGetAnchor("fnref:1", out var reference).Should().BeTrue();
        map.Text.Substring(definition.Start, definition.Length).Should().StartWith("note");
        reference.End.Should().BeLessThan(definition.Start);
    }

    [Fact]
    public void Сноску_перекрывает_расширение()
    {
        ModelDump.Of("text[^1]\n\n[^1]: note", new FootnoteAsTip()).Should().Be("P(text{TipVm}) _ {HiddenVm}");
    }

    [Fact]
    public void Картинка_грузится_загрузчиком_вьюера_от_BaseUri()
    {
        WpfHost.Invoke(() =>
        {
            var loader = new RecordingLoader(10, 20);
            var viewer = new MarkdownViewer { ImageLoader = loader, BaseUri = new("file:///C:/docs/"), Markdown = "![a](img/p.png)" };
            var window = WpfHost.Show(viewer);

            loader.Calls.Should().ContainSingle().Which.Should().Be(("img/p.png", new("file:///C:/docs/img/p.png")));
            var image = Visuals.Descendants<MdImage>(viewer).Single();
            image.ImageSource.Should().NotBeNull();
            image.ActualWidth.Should().Be(10);
            image.ActualHeight.Should().Be(20);
            window.Close();
        });
    }

    [Fact]
    public void Широкая_картинка_не_шире_текста_и_пропорциональна()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { ImageLoader = new RecordingLoader(2000, 100), Markdown = "![wide](w.png)" };
            var window = WpfHost.Show(viewer);

            var image = Visuals.Descendants<MdImage>(viewer).Single();
            var text = Visuals.Descendants<MdTextBlock>(viewer).Single();
            image.ActualWidth.Should().BeLessThanOrEqualTo(text.ActualWidth + 0.5);
            (image.ActualWidth / image.ActualHeight).Should().BeApproximately(20, 0.1);
            window.Close();
        });
    }

    [Fact]
    public void Загруженная_картинка_берётся_из_кэша()
    {
        WpfHost.Invoke(() =>
        {
            var loader = new RecordingLoader(10, 10);
            var first = new MarkdownViewer { ImageLoader = loader, Markdown = "![a](same.png)" };
            var second = new MarkdownViewer { ImageLoader = loader, Markdown = "![b](same.png)" };
            var window = WpfHost.Show(new StackPanel { Children = { first, second } });

            loader.Calls.Should().ContainSingle();
            Visuals.Descendants<MdImage>(second).Single().ImageSource.Should().NotBeNull();
            window.Close();
        });
    }

    [Fact]
    public void Исключение_загрузчика_оставляет_alt()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { ImageLoader = new ThrowingLoader(), Markdown = "![company logo](logo.png)" };
            var window = WpfHost.Show(viewer);

            var image = Visuals.Descendants<MdImage>(viewer).Single();
            image.ImageSource.Should().BeNull();
            var alt = (TextBlock)image.Template.FindName("Alt", image);
            (alt.Text, alt.Visibility).Should().Be(("company logo", Visibility.Visible));
            window.Close();
        });
    }

    [Fact]
    public void Задача_рисуется_картинкой_флажка_а_не_контролом()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "- [ ] todo\n- [x] done" };
            var window = WpfHost.Show(viewer);

            Visuals.Descendants<MdTaskMark>(viewer).Select(mark => mark.IsChecked).Should().Equal(false, true);
            Visuals.Descendants<CheckBox>(viewer).Should().BeEmpty();
            window.Close();
        });
    }

    [Fact]
    public void Флажок_задачи_для_автоматизации_недоступный_флажок_с_её_состоянием()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "- [x] done" };
            var window = WpfHost.Show(viewer);
            var peer = UIElementAutomationPeer.CreatePeerForElement(Visuals.Descendants<MdTaskMark>(viewer).Single());

            peer.GetAutomationControlType().Should().Be(AutomationControlType.CheckBox);
            ((IToggleProvider)peer.GetPattern(PatternInterface.Toggle)).ToggleState.Should().Be(System.Windows.Automation.ToggleState.On);
            peer.IsEnabled().Should().BeFalse();
            window.Close();
        });
    }

    [Fact]
    public void Ячейки_выравниваются_по_столбцам()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "| left | center | right |\n|:--|:-:|--:|\n| a | b | c |" };
            var window = WpfHost.Show(viewer);

            Visuals.Descendants<MdTextBlock>(viewer)
                .Select(text => text.TextAlignment)
                .Should().Equal(
                    TextAlignment.Left,
                    TextAlignment.Center,
                    TextAlignment.Right,
                    TextAlignment.Left,
                    TextAlignment.Center,
                    TextAlignment.Right);
            window.Close();
        });
    }

    [Fact]
    public void Ссылка_сноски_ведёт_к_определению_и_обратно()
    {
        WpfHost.Invoke(() =>
        {
            var filler = string.Join("\n\n", Enumerable.Range(0, 80).Select(i => $"Paragraph {i}"));
            var viewer = new MarkdownViewer { Markdown = $"text[^1]\n\n{filler}\n\n[^1]: note\n\n{filler}" };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);

            Click(Visuals.Descendants<MdFootnoteLink>(viewer).Single(link => !link.Link!.IsBack));
            WpfHost.Settle(window);
            viewer.TextMap.TryGetAnchor("fn:1", out var definition).Should().BeTrue();
            TopOf(viewer, scroll, definition).Should().BeApproximately(0, 1);

            Click(Visuals.Descendants<MdFootnoteLink>(viewer).Single(link => link.Link!.IsBack));
            WpfHost.Settle(window);
            scroll.VerticalOffset.Should().BeApproximately(0, 1);
            window.Close();
        });
    }

    [Fact]
    public void Клавиша_рисуется_рамкой_и_копируется_текстом()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "Press <kbd>Ctrl</kbd>+<kbd>C</kbd>" };
            var window = WpfHost.Show(viewer);
            var text = Visuals.Descendants<MdTextBlock>(viewer).Single();

            Visuals.Descendants<InlineCodePresenter>(viewer).Should().HaveCount(2).And.OnlyContain(presenter => presenter.Style == text.KbdStyle);
            viewer.SelectAll();
            viewer.SelectedText.Should().Be("Press Ctrl+C");
            viewer.SelectedMarkdown.Should().Be("Press <kbd>Ctrl</kbd>+<kbd>C</kbd>");
            window.Close();
        });
    }

    private static IReadOnlyList<InlineVm> Inlines(string markdown)
        => ModelBuilder.Parse(markdown).OfType<ParagraphVm>().Single().Inlines;

    private static double TopOf(MarkdownViewer viewer, ScrollViewer scroll, MdTextRange range)
        => viewer.TranslatePoint(viewer.GetRangeRects(range)[0].TopLeft, scroll).Y;

    private static void Click(UIElement element)
    {
        element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent });
        element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonUpEvent });
    }

    private sealed class RecordingLoader(int width, int height) : IMdImageLoader
    {
        public List<(string Url, Uri? Uri)> Calls { get; } = [];

        public Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken)
        {
            Calls.Add((url, uri));
            var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, new byte[width * height * 4], width * 4);
            bitmap.Freeze();
            return Task.FromResult<ImageSource?>(bitmap);
        }
    }

    private sealed class ThrowingLoader : IMdImageLoader
    {
        public Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken)
            => throw new InvalidOperationException("The loader failed.");
    }

    private sealed record TipVm(string Tip) : InlineVm;

    private sealed record HiddenVm : BlockVm;

    /// <summary>Сноска всплывашкой: ссылка несёт текст определения, а само определение не рисуется.</summary>
    private sealed class FootnoteAsTip : MdExtension
    {
        public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
            => inline is FootnoteLink { IsBackLink: false } link ? new TipVm(context.GetSource(link.Footnote)) : null;

        public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
            => block is Footnote ? new HiddenVm() : null;
    }
}