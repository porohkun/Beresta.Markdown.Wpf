namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Extensions;
using Kit;
using Presenters;
using Selection;
using ViewModels;

/// <summary>Готовые расширения работы над текстом: чипы, цветовой код, CriticMarkup, комментарии, спойлеры, статусы задач, сноски подсказкой.</summary>
public sealed class KitTextTests
{
    [Theory]
    [InlineData("a ::chip:: b", "Chip")]
    [InlineData("a ::ok::{.success} b", "Chip.Success")]
    [InlineData("a ::odd::{.unknown} b", "Chip")]
    public void Чип_становится_оформленным_текстом_своего_вида(string markdown, string style)
    {
        Runs(markdown, new Chips()).Where(run => run.StyleName is not null).Select(run => run.StyleName).Should().Equal(style);
    }

    [Theory]
    [InlineData("`#336699`", "#FF336699")]
    [InlineData("`#abc`", "#FFAABBCC")]
    [InlineData("`#11223344`", "#44112233")]
    [InlineData("`rgb(255, 0, 0)`", "#FFFF0000")]
    [InlineData("`rgba(0,0,255,0.5)`", "#800000FF")]
    [InlineData("`hsl(120, 100%, 50%)`", "#FF00FF00")]
    [InlineData("`hsla(0 100% 50% / 25%)`", "#40FF0000")]
    public void Код_цвета_получает_кружок(string markdown, string color)
    {
        Inlines(markdown, new ColorChips()).OfType<ColorCodeVm>().Single().Color.Should().Be(color);
    }

    [Theory]
    [InlineData("`#ggg`")]
    [InlineData("`red`")]
    [InlineData("`rgb(300, 0, 0)`")]
    [InlineData("`see #336699`")]
    public void Код_не_цвет_остаётся_кодом(string markdown)
    {
        Inlines(markdown, new ColorChips()).OfType<ColorCodeVm>().Should().BeEmpty();
    }

    [Fact]
    public void Правки_CriticMarkup_становятся_оформленным_текстом()
    {
        var inlines = Inlines("{++add++} {--cut--} {~~old~>new~~} {==mark==}{>>note<<}", new CriticMarkup());

        inlines.OfType<RunVm>().Where(run => run.StyleName is not null).Select(run => (run.Text, run.StyleName))
            .Should().Equal(
                ("add", "Critic.Insertion"),
                ("cut", "Critic.Deletion"),
                ("old", "Critic.Deletion"),
                ("new", "Critic.Insertion"),
                ("mark", "Critic.Highlight"));
        inlines.OfType<CriticCommentVm>().Single().Text.Should().Be("note");
    }

    [Theory]
    [InlineData("{++add++} and {--cut--}")]
    [InlineData("x {~~old~>new~~} y")]
    [InlineData("{==mark==}{>>note<<} end")]
    public void Правки_пишутся_в_разметку_как_были(string markdown)
    {
        Copy(markdown, new CriticMarkup()).Should().Be(markdown);
    }

    [Fact]
    public void У_правок_нет_кнопок_решения()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "Keep {~~old~>new~~} text." };
            viewer.Extensions.Add(new CriticMarkup());
            var window = WpfHost.Show(viewer);

            Visuals.Descendants<Button>(viewer).Should().BeEmpty();
            viewer.TextMap.Text.Should().Be("Keep oldnew text.");
            window.Close();
        });
    }

    [Theory]
    [InlineData("a %%hidden%% b", "P(a  b)")]
    [InlineData("%%\nline1\n\nline2\n%%\n\ntext", "P(text)")]
    [InlineData("before\n\n%%\nx\n%%\n\nafter", "P(before) _ _ P(after)")]
    [InlineData("%% one line %% text", "P( text)")]
    public void Комментарии_не_рисуются(string markdown, string expected)
    {
        ModelDump.Of(markdown, new Comments()).Should().Be(expected);
    }

    [Fact]
    public void Спойлер_становится_оформленным_текстом_и_пишется_как_был()
    {
        const string markdown = "a ||secret|| b";

        Runs(markdown, new Spoilers()).Single(run => run.StyleName == "Spoiler").Text.Should().Be("secret");
        Copy(markdown, new Spoilers()).Should().Be(markdown);
    }

    [Fact]
    public void В_строке_таблицы_спойлера_нет()
    {
        var blocks = Parse("| a | b |\n|---|---|\n| ||x|| | y |", new Spoilers());

        blocks.OfType<TableVm>().Should().ContainSingle();
        blocks.OfType<TableVm>().Single().Rows.SelectMany(row => row.Cells).SelectMany(cell => cell.Inlines).OfType<RunVm>()
            .Should().NotContain(run => run.StyleName == "Spoiler");
    }

    [Fact]
    public void Щелчок_раскрывает_спойлер_и_раскрытость_переживает_новый_текст()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "a ||secret|| b" };
            viewer.Extensions.Add(new Spoilers());
            var window = WpfHost.Show(viewer);
            var cover = (Brush)viewer.FindResource("Md.Spoiler.Cover");

            Spoiler(viewer).Foreground.Should().BeSameAs(cover);
            Spoiler(viewer).RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonUpEvent });
            Spoiler(viewer).Foreground.Should().NotBeSameAs(cover);

            viewer.Markdown = "Above.\n\n" + viewer.Markdown;
            WpfHost.Settle(window);
            Spoiler(viewer).Foreground.Should().NotBeSameAs(cover);
            window.Close();
        });
    }

    [Fact]
    public void Статусы_задач_становятся_задачами_со_статусом()
    {
        var tasks = Parse("- [/] doing\n- [x] done\n- [?] maybe\n- text [/] inside", new TaskStatuses())
            .OfType<ListVm>().Single().Items
            .SelectMany(item => item.Blocks.OfType<ParagraphVm>().SelectMany(paragraph => paragraph.Inlines))
            .OfType<TaskVm>()
            .Select(task => task.Status)
            .ToList();

        tasks.Should().Equal('/', null, '?');
        Copy("- [/] doing\n- [?] maybe", new TaskStatuses()).Should().Be("- [/] doing\n- [?] maybe");
    }

    [Fact]
    public void Статус_задачи_рисуется_знаком_в_том_же_квадрате_что_и_флажок()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "- [>] later\n- [ ] todo" };
            viewer.Extensions.Add(new TaskStatuses());
            var window = WpfHost.Show(viewer);

            var marks = Visuals.Descendants<MdTaskMark>(viewer).ToList();
            marks.Should().HaveCount(2);
            marks[0].Glyph.Should().BeSameAs(viewer.FindResource("Md.TaskStatus.Forwarded.Icon"));
            marks[1].Glyph.Should().BeNull();
            marks[0].RenderSize.Should().Be(marks[1].RenderSize);
            var peer = UIElementAutomationPeer.CreatePeerForElement(marks[0]);
            ((IToggleProvider)peer.GetPattern(PatternInterface.Toggle)).ToggleState.Should().Be(ToggleState.Indeterminate);

            viewer.Resources["Md.Task.Brush"] = Brushes.Red;
            WpfHost.Settle(window);

            marks.Select(mark => mark.Brush).Should().OnlyContain(brush => brush == Brushes.Red);
            window.Close();
        });
    }

    [Fact]
    public void Сноска_подсказкой_прячет_определение()
    {
        var blocks = Parse("Text[^1].\n\n[^1]: The **note**.", new FootnoteTips());

        blocks.OfType<FootnoteVm>().Should().BeEmpty();
        blocks.OfType<ParagraphVm>().Single().Inlines.OfType<FootnoteTipVm>().Single()
            .Should().Be(new FootnoteTipVm(1, "The **note**.") { Source = blocks.OfType<ParagraphVm>().Single().Inlines.OfType<FootnoteTipVm>().Single().Source, NodeId = blocks.OfType<ParagraphVm>().Single().Inlines.OfType<FootnoteTipVm>().Single().NodeId });
    }

    private static Span Spoiler(MarkdownViewer viewer)
    {
        var style = (Style)viewer.FindResource("Md.Spoiler.Style");
        return Visuals.Descendants<MdTextBlock>(viewer).SelectMany(text => Spans(text.Inlines)).Single(span => ReferenceEquals(span.Style, style));
    }

    private static IEnumerable<Span> Spans(InlineCollection inlines)
        => inlines.OfType<Span>().SelectMany(span => Spans(span.Inlines).Prepend(span));

    private static IReadOnlyList<BlockVm> Parse(string markdown, MdExtension extension)
        => ModelBuilder.Parse(markdown, ModelDump.Pipeline(extension), [extension]);

    private static IReadOnlyList<InlineVm> Inlines(string markdown, MdExtension extension)
        => Parse(markdown, extension).OfType<ParagraphVm>().Single().Inlines;

    private static List<RunVm> Runs(string markdown, MdExtension extension)
        => Inlines(markdown, extension).OfType<RunVm>().ToList();

    private static string Copy(string markdown, MdExtension extension)
    {
        var blocks = Parse(markdown, extension);
        var map = MdTextMap.Build(blocks);
        return MdMarkdownWriter.Write(blocks, map, markdown, new(0, map.Length));
    }
}
