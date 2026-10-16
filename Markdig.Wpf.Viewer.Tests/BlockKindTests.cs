namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Markdig.Extensions.CustomContainers;
using Presenters;
using Selection;
using ViewModels;

/// <summary>Плашки, сворачиваемые блоки, списки определений и front matter.</summary>
public sealed class BlockKindTests
{
    private const string Definitions = "Term\n:   Definition one\n:   Definition two\n\nTerm 2\nTerm 2b\n:   Def";

    [Fact]
    public void Плашки_GitHub_становятся_плашками_своего_вида()
    {
        ModelDump.Of("> [!NOTE]\n> Useful.\n\n> [!WARNING]\n> Careful.")
            .Should().Be("Callout:note[P(Useful.)] _ Callout:warning[P(Careful.)]");
    }

    [Fact]
    public void Плашка_рисуется_цветом_значком_и_заголовком_вида()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "> [!TIP]\n> text" };
            var window = WpfHost.Show(viewer);
            var callout = Visuals.Descendants<MdCalloutPresenter>(viewer).Single();

            callout.DefaultTitle.Should().Be("Tip");
            callout.Accent.Should().BeSameAs(viewer.FindResource("Md.Callout.Tip.Brush"));
            callout.Icon.Should().BeSameAs(viewer.FindResource("Md.Callout.Tip.Icon"));
            UIElementAutomationPeer.CreatePeerForElement(callout).GetName().Should().Be("Tip");
            window.Close();
        });
    }

    [Fact]
    public void Незнакомый_вид_рисуется_заметкой_с_названием_вида()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "> [!kitten]\n> meow" };
            var window = WpfHost.Show(viewer);
            var callout = Visuals.Descendants<MdCalloutPresenter>(viewer).Single();

            callout.DefaultTitle.Should().Be("Kitten");
            callout.Accent.Should().BeSameAs(viewer.FindResource("Md.Callout.Note.Brush"));
            window.Close();
        });
    }

    [Fact]
    public void Свой_вид_плашки_задаётся_ключами_темы()
    {
        WpfHost.Invoke(() =>
        {
            var brush = new SolidColorBrush(Colors.Teal);
            var viewer = new MarkdownViewer { Markdown = "> [!question]\n> why\n\n> [!idea]\n> what" };
            viewer.Resources["Md.Callout.Question.Brush"] = brush;
            viewer.Resources["Md.Callout.Idea.Title"] = "Идея";
            var window = WpfHost.Show(viewer);
            var callouts = Visuals.Descendants<MdCalloutPresenter>(viewer).ToList();

            callouts[0].Accent.Should().BeSameAs(brush);
            callouts[0].Icon.Should().BeNull();
            callouts[0].DefaultTitle.Should().Be("Question");
            callouts[1].Accent.Should().BeSameAs(viewer.FindResource("Md.Callout.Note.Brush"));
            callouts[1].DefaultTitle.Should().Be("Идея");
            window.Close();
        });
    }

    [Fact]
    public void Свой_заголовок_плашки_заменяет_заголовок_вида()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ":::fold\nhidden\n:::" };
            viewer.Extensions.Add(new LambdaExtension
            {
                Block = (block, context) => block is CustomContainer container
                    ? new CalloutVm("warning", [new RunVm("Own title")], context.ConvertChildren(container))
                    : null
            });
            var window = WpfHost.Show(viewer);
            var callout = Visuals.Descendants<MdCalloutPresenter>(viewer).Single();

            ((FrameworkElement)callout.Template.FindName("DefaultTitle", callout)).Visibility.Should().Be(Visibility.Collapsed);
            UIElementAutomationPeer.CreatePeerForElement(callout).GetName().Should().Be("Own title");
            viewer.SelectAll();
            viewer.SelectedText.Should().Be("Own title\nhidden");
            window.Close();
        });
    }

    [Fact]
    public void Сворачиваемая_плашка_помнит_свёрнутость_через_новый_текст()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ":::fold\nhidden\n:::" };
            viewer.Extensions.Add(Folding());
            var window = WpfHost.Show(viewer);

            Body(Visuals.Descendants<MdCalloutPresenter>(viewer).Single()).Visibility.Should().Be(Visibility.Collapsed);
            Header(Visuals.Descendants<MdCalloutPresenter>(viewer).Single()).IsChecked = true;
            WpfHost.Settle(window);
            Body(Visuals.Descendants<MdCalloutPresenter>(viewer).Single()).Visibility.Should().Be(Visibility.Visible);

            viewer.Markdown = "Above.\n\n" + viewer.Markdown;
            WpfHost.Settle(window);
            Body(Visuals.Descendants<MdCalloutPresenter>(viewer).Single()).Visibility.Should().Be(Visibility.Visible);
            window.Close();
        });
    }

    [Fact]
    public void Сворачиваемый_блок_свёрнут_пока_его_не_развернут()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ":::details Summary text\ninside\n:::" };
            viewer.Extensions.Add(DetailsFromContainer());
            var window = WpfHost.Show(viewer);
            var details = Visuals.Descendants<MdDetailsPresenter>(viewer).Single();
            var body = (FrameworkElement)details.Template.FindName("Body", details);

            body.Visibility.Should().Be(Visibility.Collapsed);
            var peer = UIElementAutomationPeer.CreatePeerForElement(details);
            peer.GetName().Should().Be("Summary text");
            var expander = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse);
            expander.ExpandCollapseState.Should().Be(ExpandCollapseState.Collapsed);
            expander.Expand();
            WpfHost.Settle(window);

            body.Visibility.Should().Be(Visibility.Visible);
            viewer.SelectAll();
            viewer.SelectedText.Should().Be("Summary text\ninside");
            window.Close();
        });
    }

    [Fact]
    public void Список_определений_держит_термины_и_определения()
    {
        ModelDump.Of(Definitions)
            .Should().Be("DL[Term: P(Definition one); : P(Definition two) _; Term 2\nTerm 2b: P(Def)]");
        MdTextMap.Build(ModelBuilder.Parse(Definitions)).Text.Should().StartWith("Term\nDefinition one");
    }

    [Fact]
    public void Термин_рисуется_своим_стилем()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = Definitions };
            var window = WpfHost.Show(viewer);
            var term = Visuals.Descendants<MdTextBlock>(viewer).First(text => text.Runs?.OfType<RunVm>().FirstOrDefault()?.Text == "Term");

            Visuals.Descendants<MdDefinitionListPresenter>(viewer).Should().ContainSingle();
            term.Style.Should().BeSameAs(viewer.FindResource("Md.DefinitionList.TermStyle"));
            window.Close();
        });
    }

    [Fact]
    public void Front_matter_не_рисуется_и_отдаётся_текстом_а_строки_не_съезжают()
    {
        const string markdown = "---\ntitle: Hello\ntags: [a, b]\n---\n\n# Doc";
        var blocks = ModelBuilder.Parse(markdown);

        ModelDump.Of(blocks).Should().Be("H1(Doc)");
        blocks.Single().Source.StartLine.Should().Be(5);
        MdTextMap.Build(blocks).RangeOfSourceLines(0, 3).Should().BeNull();

        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = markdown };
            var window = WpfHost.Show(viewer);

            viewer.FrontMatter.Should().Be("title: Hello\ntags: [a, b]");
            viewer.Markdown = "# No front matter";
            viewer.FrontMatter.Should().BeNull();
            window.Close();
        });
    }

    [Theory]
    [InlineData("> [!NOTE]\n> Useful.", 1, "Useful.")]
    [InlineData(Definitions, 5, "Term 2b")]
    [InlineData(Definitions, 6, "Def")]
    [InlineData("---\na: b\n---\n\ntext", 4, "text")]
    public void Строка_исходника_и_текст_нового_блока_находят_друг_друга(string markdown, int line, string text)
    {
        var map = MdTextMap.Build(ModelBuilder.Parse(markdown));

        var range = map.RangeOfSourceLines(line, line);

        range.Should().NotBeNull();
        map.GetText(range!.Value).Should().Be(text);
        map.SourceLineOf(map.Text.LastIndexOf(text, StringComparison.Ordinal)).Should().Be(line);
    }

    private static LambdaExtension Folding() => new()
    {
        Block = (block, context) => block is CustomContainer { Info: "fold" } container
            ? new CalloutVm("tip", [], context.ConvertChildren(container)) { IsFoldable = true, IsFolded = true }
            : null
    };

    private static LambdaExtension DetailsFromContainer() => new()
    {
        Block = (block, context) => block is CustomContainer { Info: "details" } container
            ? new DetailsVm([new RunVm(container.Arguments ?? string.Empty)], context.ConvertChildren(container))
            : null
    };

    private static FrameworkElement Body(MdCalloutPresenter callout) => (FrameworkElement)callout.Template.FindName("Body", callout);

    private static ToggleButton Header(MdCalloutPresenter callout) => (ToggleButton)callout.Template.FindName("Header", callout);
}
