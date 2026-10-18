namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Identity;
using Presenters;
using Selection;
using ViewModels;

/// <summary>HTML из README: сворачиваемые блоки, выравнивание, заголовки и картинки HTML-блоком.</summary>
public sealed class ReadmeHtmlTests
{
    private const string Details = "<details>\n<summary>Title</summary>\n\nMarkdown **x**\n\n</details>";

    [Theory]
    [InlineData(Details, "Det(Title)[P(Markdown x)]")]
    [InlineData("<details open><summary>T</summary>\n\ntext\n</details>", "Det(T)[P(text)]")]
    [InlineData("<details>\n<summary>T</summary>\nplain\n</details>", "Det(T)[P(plain)]")]
    [InlineData("<details><summary>One line</summary>Body text</details>", "Det(One line)[P(Body text)]")]
    [InlineData("<details>\n\nno summary\n\n</details>", "Det()[P(no summary)]")]
    [InlineData("<details>\n<summary>A</summary>\n\na\n\n</details>\n<details>\n<summary>B</summary>\n\nb\n\n</details>", "Det(A)[P(a)] Det(B)[P(b)]")]
    [InlineData("<details>\n<summary>A</summary>\n\n- item\n\n<details>\n<summary>B</summary>\n\ninner\n\n</details>\n</details>", "Det(A)[L[• P(item)] _ Det(B)[P(inner)]]")]
    [InlineData("<details>\n<summary>Open</summary>\n\nto the end", "Det(Open)[P(to the end)]")]
    public void Details_и_summary_становятся_сворачиваемым_блоком(string markdown, string expected)
    {
        ModelDump.Of(markdown).Should().Be(expected);
    }

    [Fact]
    public void Атрибут_open_разворачивает_блок_сразу()
    {
        ModelBuilder.Parse("<details open><summary>T</summary>\n\ntext\n</details>").OfType<DetailsVm>().Single().IsOpen.Should().BeTrue();
        ModelBuilder.Parse(Details).OfType<DetailsVm>().Single().IsOpen.Should().BeFalse();
    }

    [Theory]
    [InlineData("<p align=\"center\">\n  <img src=\"logo.png\" width=\"200\">\n  <br>\n  <b>Tag</b>\n</p>", "Center[P(![]\nTag)]")]
    [InlineData("<div align=\"center\">\n\n# Title\n\ntext\n\n</div>", "Center[H1(Title) _ P(text)]")]
    [InlineData("<h1 align=\"center\">Title</h1>", "Center[H1(Title)]")]
    [InlineData("<h2>Plain</h2>", "H2(Plain)")]
    [InlineData("<center>\n\nCentered **md**\n\n</center>", "Center[P(Centered md)]")]
    [InlineData("<div align=\"right\">\n  <img src=\"x.png\">\n</div>", "Right[P(![])]")]
    [InlineData("<div>text</div>", "P(text)")]
    [InlineData("<p>a &amp; b\n   c</p>", "P(a & b c)")]
    public void Выравнивание_и_заголовки_HTML(string markdown, string expected)
    {
        ModelDump.Of(markdown).Should().Be(expected);
    }

    [Theory]
    [InlineData("<img src=\"x.png\" width=\"100\">", "x.png", null)]
    [InlineData("<a href=\"https://x.example\">\n  <img src=\"y.svg\">\n</a>", "y.svg", "https://x.example")]
    [InlineData("<picture>\n  <source media=\"(prefers-color-scheme: dark)\" srcset=\"d.png\">\n  <img alt=\"L\" src=\"l.png\">\n</picture>", "l.png", null)]
    public void Картинка_HTML_блоком_рисуется_картинкой(string markdown, string url, string? href)
    {
        var image = ModelBuilder.Parse(markdown).OfType<ParagraphVm>().Single().Inlines.OfType<ImageVm>().Single();

        image.Url.Should().Be(url);
        image.Href.Should().Be(href);
    }

    [Fact]
    public void Ряд_значков_по_центру_держит_ссылки_и_пробелы()
    {
        var aligned = ModelBuilder.Parse("<p align=\"center\"><a href=\"x\"><img src=\"a.svg\"></a> <a href=\"y\"><img src=\"b.svg\"></a></p>").OfType<AlignedVm>().Single();
        var inlines = aligned.Blocks.OfType<ParagraphVm>().Single().Inlines;

        ModelDump.Text(inlines).Should().Be("![] ![]");
        inlines.OfType<ImageVm>().Select(image => image.Href).Should().Equal("x", "y");
    }

    [Fact]
    public void Начертания_и_перенос_в_HTML_блоке()
    {
        var inlines = ModelBuilder.Parse("<p><b>bold</b> and <code>code</code><br>next</p>").OfType<ParagraphVm>().Single().Inlines;

        inlines.OfType<RunVm>().Single(run => run.Text == "bold").Bold.Should().BeTrue();
        inlines.OfType<RunVm>().Single(run => run.Text == "code").Code.Should().BeTrue();
        ModelDump.Text(inlines).Should().Be("bold and code\nnext");
    }

    [Theory]
    [InlineData("<table>\n<tr><td>x</td></tr>\n</table>")]
    [InlineData("<p align=\"center\"><span>x</span></p>")]
    public void HTML_блок_с_незнакомым_тегом_остаётся_текстом(string markdown)
    {
        ModelDump.Of(markdown).Should().Be($"P({markdown})");
    }

    [Theory]
    [InlineData(Details, 1, "Title")]
    [InlineData(Details, 3, "Markdown x")]
    [InlineData("<p align=\"center\">\n  <b>Tag</b>\n</p>", 1, "Tag")]
    public void Строка_HTML_ведёт_к_своему_тексту(string markdown, int line, string text)
    {
        var map = MdTextMap.Build(ModelBuilder.Parse(markdown));

        var range = map.RangeOfSourceLines(line, line);

        range.Should().NotBeNull();
        map.GetText(range!.Value).Should().Be(text);
        map.SourceLineOf(map.Text.IndexOf(text, StringComparison.Ordinal)).Should().Be(line);
    }

    [Fact]
    public void Блоки_одного_HTML_блока_держат_свои_идентификаторы_через_новый_текст()
    {
        var identity = new ModelIdentity();
        var before = ModelBuilder.Parse("<p>a</p>\n<p>b</p>", null, null, identity);
        var after = ModelBuilder.Parse("Above\n\n<p>a</p>\n<p>b</p>", null, null, identity);

        before.Select(block => block.NodeId).Should().OnlyHaveUniqueItems().And.NotContain(0);
        after.OfType<ParagraphVm>().Skip(1).Select(block => block.NodeId).Should().Equal(before.Select(block => block.NodeId));
    }

    [Fact]
    public void Сворачиваемый_блок_HTML_помнит_развёрнутость_через_новый_текст()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = Details + "\n\n<details open>\n<summary>Second</summary>\n\nshown\n\n</details>" };
            var window = WpfHost.Show(viewer);
            var blocks = Visuals.Descendants<MdDetailsPresenter>(viewer).ToList();

            blocks.Select(details => details.IsExpanded).Should().Equal(false, true);
            var peer = (IExpandCollapseProvider)UIElementAutomationPeer.CreatePeerForElement(blocks[0]).GetPattern(PatternInterface.ExpandCollapse);
            peer.Expand();
            viewer.Markdown = "Above.\n\n" + viewer.Markdown;
            WpfHost.Settle(window);

            Visuals.Descendants<MdDetailsPresenter>(viewer).Select(details => details.IsExpanded).Should().Equal(true, true);
            peer.ExpandCollapseState.Should().Be(ExpandCollapseState.Expanded);
            window.Close();
        });
    }

    [Fact]
    public void Выравнивание_доходит_до_текста_а_код_остаётся_слева()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "<div align=\"center\">\n\ntext\n\n```\ncode\n```\n\n</div>" };
            var window = WpfHost.Show(viewer);
            var texts = Visuals.Descendants<MdTextBlock>(viewer).ToList();

            texts.Single(text => text.Runs?.OfType<RunVm>().FirstOrDefault()?.Text == "text").TextAlignment.Should().Be(TextAlignment.Center);
            texts.Single(text => text.Name == "PART_Code").TextAlignment.Should().Be(TextAlignment.Left);
            window.Close();
        });
    }
}
