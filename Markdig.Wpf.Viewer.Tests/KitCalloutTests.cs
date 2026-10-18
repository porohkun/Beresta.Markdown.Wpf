namespace MarkdigWpfViewer.Tests;

using System.Windows;
using Extensions;
using Kit;
using Markdig.Syntax;
using Presenters;
using Selection;
using ViewModels;
using static TestWidgets;

/// <summary>Готовые расширения плашек и сворачиваемого: Obsidian, MkDocs, контейнеры <c>:::вид</c>.</summary>
public sealed class KitCalloutTests
{
    [Theory]
    [InlineData("> [!tip] My title\n> body", "Callout:tip(My title)[P(body)]")]
    [InlineData("> [!info]\n> body", "Callout:info[P(body)]")]
    [InlineData("> [!faq]+\n> body", "Callout:question(Faq)[P(body)]")]
    [InlineData("> [!quote] Only title", "Callout:quote(Only title)[]")]
    [InlineData("> [!warning] Title with **bold**\n> body\n>\n> second", "Callout:warning(Title with bold)[P(body) _ P(second)]")]
    [InlineData("> [!note]\n> > [!tip] nested\n> > inner", "Callout:note[Callout:tip(nested)[P(inner)]]")]
    [InlineData("> [!kitten] Meow\n> purr", "Callout:kitten(Meow)[P(purr)]")]
    [InlineData("> plain quote", "Q[P(plain quote)]")]
    public void Плашки_Obsidian(string markdown, string expected)
    {
        ModelDump.Of(markdown, new ObsidianCallouts()).Should().Be(expected);
    }

    [Fact]
    public void Плашка_Obsidian_остаётся_цитатой_когда_абзац_забрало_другое_расширение()
    {
        var paragraphs = new LambdaExtension { Block = (block, _) => block is ParagraphBlock ? new DiagramVm("paragraph") : null };

        ModelDump.Of("> [!note] Title\n> body", paragraphs, new ObsidianCallouts()).Should().Be("Q[{DiagramVm}]");
    }

    [Theory]
    [InlineData("> [!bug]- Title\n> x", true, true)]
    [InlineData("> [!bug]+ Title\n> x", true, false)]
    [InlineData("> [!bug] Title\n> x", false, false)]
    public void Знак_после_вида_делает_плашку_сворачиваемой(string markdown, bool foldable, bool folded)
    {
        var callout = Parse(markdown, new ObsidianCallouts()).OfType<CalloutVm>().Single();

        (callout.IsFoldable, callout.IsFolded).Should().Be((foldable, folded));
    }

    [Theory]
    [InlineData("!!! note \"Title\"\n    body text\n\n    second para", "Callout:note(Title)[P(body text) _ P(second para)]")]
    [InlineData("??? tip\n    collapsed", "Callout:tip[P(collapsed)]")]
    [InlineData("???+ warning \"Open\"\n    expanded", "Callout:warning(Open)[P(expanded)]")]
    [InlineData("!!! danger\n    x\n\ntext after", "Callout:danger[P(x)] _ P(text after)")]
    [InlineData("!!! example\n    ```cs\n    code\n    ```", "Callout:example[C(code)]")]
    [InlineData("!!! tldr \"\"\n    short", "Callout:abstract(Tldr)[P(short)]")]
    [InlineData("!!! note inline end \"Side\"\n    aside", "Callout:note(Side)[P(aside)]")]
    public void Предупреждения_MkDocs(string markdown, string expected)
    {
        ModelDump.Of(markdown, new MkDocsAdmonitions()).Should().Be(expected);
    }

    [Theory]
    [InlineData("??? tip\n    x", true, true)]
    [InlineData("???+ tip\n    x", true, false)]
    [InlineData("!!! tip\n    x", false, false)]
    public void Вопросы_MkDocs_делают_предупреждение_сворачиваемым(string markdown, bool foldable, bool folded)
    {
        var callout = Parse(markdown, new MkDocsAdmonitions()).OfType<CalloutVm>().Single();

        (callout.IsFoldable, callout.IsFolded).Should().Be((foldable, folded));
    }

    [Fact]
    public void Строки_предупреждения_MkDocs_ведут_к_своему_тексту()
    {
        var map = MdTextMap.Build(Parse("!!! note \"Title\"\n    body text\n\n    second", new MkDocsAdmonitions()));

        map.GetText(map.RangeOfSourceLines(1, 1)!.Value).Should().Be("body text");
        map.SourceLineOf(map.Text.IndexOf("second", StringComparison.Ordinal)).Should().Be(3);
    }

    [Theory]
    [InlineData(":::tip Title here\nbody\n:::", "Callout:tip(Title here)[P(body)]")]
    [InlineData(":::tip[Bracket title]\nbody\n:::", "Callout:tip(Bracket title)[P(body)]")]
    [InlineData("::: warning\nspaced\n:::", "Callout:warning[P(spaced)]")]
    [InlineData(":::details Click me\nhidden\n:::", "Det(Click me)[P(hidden)]")]
    [InlineData(":::spoiler\nsp\n:::", "Det()[P(sp)]")]
    [InlineData(":::hint\nh\n:::", "Callout:tip(Hint)[P(h)]")]
    [InlineData(":::foo\nunknown\n:::", "P(unknown)")]
    public void Контейнеры_с_видом(string markdown, string expected)
    {
        ModelDump.Of(markdown, new CalloutContainers()).Should().Be(expected);
    }

    [Fact]
    public void Спойлер_HackMD_с_open_развёрнут()
    {
        var details = Parse(":::spoiler {state=\"open\"} Title\nx\n:::", new CalloutContainers()).OfType<DetailsVm>().Single();

        details.IsOpen.Should().BeTrue();
        ModelDump.Text(details.Summary).Should().Be("Title");
    }

    [Theory]
    [InlineData("> [!tip] My title\n> body", "> [!TIP] My title\n> body")]
    [InlineData("> [!bug]- Title\n> x", "> [!BUG]- Title\n> x")]
    [InlineData("!!! note \"Title\"\n    body", "> [!NOTE] Title\n> body")]
    public void Плашки_готовых_расширений_пишутся_синтаксисом_Obsidian(string markdown, string expected)
    {
        MdExtension[] extensions = [new ObsidianCallouts(), new MkDocsAdmonitions()];
        var blocks = ModelBuilder.Parse(markdown, ModelDump.Pipeline(extensions), extensions);
        var map = MdTextMap.Build(blocks);

        MdMarkdownWriter.Write(blocks, map, markdown, new(0, map.Length)).Should().Be(expected);
    }

    [Fact]
    public void Вид_Obsidian_рисуется_своим_цветом_значком_и_заголовком()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "> [!success]\n> ok\n\n> [!faq] \n> q\n\n> [!bug]- Folded\n> hidden" };
            viewer.Extensions.Add(new ObsidianCallouts());
            var window = WpfHost.Show(viewer);
            var callouts = Visuals.Descendants<MdCalloutPresenter>(viewer).ToList();

            callouts[0].Accent.Should().BeSameAs(viewer.FindResource("Md.Callout.Success.Brush"));
            callouts[0].Icon.Should().BeSameAs(viewer.FindResource("Md.Callout.Success.Icon"));
            callouts[0].DefaultTitle.Should().Be("Success");
            callouts[1].Accent.Should().BeSameAs(viewer.FindResource("Md.Callout.Question.Brush"));
            callouts[1].TitleText.Should().Be("Faq");
            ((FrameworkElement)callouts[2].Template.FindName("Body", callouts[2])).Visibility.Should().Be(Visibility.Collapsed);
            window.Close();
        });
    }

    private static IReadOnlyList<BlockVm> Parse(string markdown, MdExtension extension)
        => ModelBuilder.Parse(markdown, ModelDump.Pipeline(extension), [extension]);
}
