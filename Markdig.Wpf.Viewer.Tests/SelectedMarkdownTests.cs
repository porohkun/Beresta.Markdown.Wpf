namespace MarkdigWpfViewer.Tests;

using Selection;
using ViewModels;

/// <summary>Разметка выделенного: выделенный кусок, записанный markdown со своим оформлением.</summary>
public sealed class SelectedMarkdownTests
{
    [Theory]
    [InlineData("Абзац с **жирным словом** и [ссылкой](https://example.com).", "с жирным сл", "с **жирным сл**")]
    [InlineData("see [the docs](https://x.example/a) now", "the do", "[the do](https://x.example/a)")]
    [InlineData("see [the docs](https://x.example/a \"Docs\") now", "the do", "[the do](https://x.example/a \"Docs\")")]
    [InlineData("use `foo()` here", "foo()", "`foo()`")]
    [InlineData("**bold *both* bold** tail", "bold both bold", "**bold *both* bold**")]
    [InlineData("## Title here", "Title", "Title")]
    [InlineData("| a | b |\n|---|---|\n| one | two |", "one", "one")]
    [InlineData("```cs\nfirst line\nsecond line\n```", "line", "`line`")]
    public void Кусок_одного_текста_пишется_строчной_разметкой(string markdown, string selected, string expected)
    {
        Copy(markdown, selected).Should().Be(expected);
    }

    [Theory]
    [InlineData("Абзац с **жирным словом** и [ссылкой](https://example.com).")]
    [InlineData("**bold *both* bold**")]
    [InlineData("## Title here")]
    [InlineData("# H\n\ntext\n\n- a\n- b\n\n> q")]
    [InlineData("7) a\n8) b")]
    [InlineData("- a\n  - b\n- c")]
    [InlineData("```cs\nx\ny\n```")]
    [InlineData("> a\n>\n> b")]
    [InlineData("a\n\n---\n\nb")]
    [InlineData("a\nb")]
    [InlineData("a\\\nb")]
    [InlineData("2 \\* 3 and \\_x\\_")]
    [InlineData("~~gone~~ and ***both***")]
    [InlineData("- [ ] task\n- [x] done")]
    [InlineData("snake_case and 2 * 3")]
    [InlineData("see <https://example.com>")]
    [InlineData("write to <user@example.com>")]
    [InlineData("==mark== and ++ins++ and \"\"Cite\"\"")]
    [InlineData("c. three\nd. four")]
    [InlineData("iv. four\nv. five")]
    [InlineData("^^^\n![img](a.png)\n^^^ Caption")]
    [InlineData("^^ Footer text")]
    [InlineData("> [!NOTE]\n> Useful.")]
    [InlineData("> [!CAUTION]\n> a\n>\n> b")]
    [InlineData("Term\n:   Definition one\n:   Definition two\n\nTerm 2\nTerm 2b\n:   Def")]
    [InlineData("<div align=\"center\">\n\ntext\n\n</div>")]
    [InlineData("<details>\n<summary>S</summary>\n\nx\n\n</details>")]
    [InlineData("[![Y](y.svg)](https://x.example) and [text ![i](i.png)](https://t.example)")]
    [InlineData("see [the docs](https://x.example/a \"The \\\"docs\\\"\") and [text ![i](i.png)](https://t.example \"T\")")]
    [InlineData("[![Y](y.svg)](https://x.example \"T\")")]
    [InlineData("Text with a note[^note].\n\n[^note]: The note.\n\nAfter.")]
    public void Выделенный_целиком_документ_пишется_как_был(string markdown)
    {
        CopyAll(markdown).Should().Be(markdown);
    }

    [Theory]
    [InlineData("a  \nb", "a\\\nb")]
    [InlineData("* x\n* y", "- x\n- y")]
    [InlineData("Title\n=====", "# Title")]
    [InlineData("| a | b |\n|:--|--:|\n| 1 | 2 |", "| a | b |\n| :--- | ---: |\n| 1 | 2 |")]
    [InlineData("    indented code", "```\nindented code\n```")]
    [InlineData("a &amp; b", "a & b")]
    [InlineData("# My Heading\n\nSee [My Heading].", "# My Heading\n\nSee [My Heading](#my-heading).")]
    [InlineData("```cs\r\nx\r\ny\r\n```\r\n\r\ntext\r\nmore", "```cs\nx\ny\n```\n\ntext\nmore")]
    [InlineData("x^2^ and H~2~O", "x<sup>2</sup> and H<sub>2</sub>O")]
    [InlineData("<mark>m</mark> <u>u</u> <cite>c</cite>", "==m== ++u++ \"\"c\"\"")]
    [InlineData("*[HTML]: Hyper Text\n\nHTML here", "HTML here")]
    [InlineData("+---+---+\n| a | b |\n+===+===+\n| 1 | 2 |\n|   |   |\n| x | y |\n+---+---+", "| a | b |\n| --- | --- |\n| 1<br>x | 2<br>y |")]
    [InlineData("> [!tip]\n> x", "> [!TIP]\n> x")]
    [InlineData("---\ntitle: x\n---\n\ntext", "text")]
    [InlineData("<p align=\"center\">text</p>", "<div align=\"center\">\n\ntext\n\n</div>")]
    [InlineData("<h1 align=\"center\">Title</h1>", "<div align=\"center\">\n\n# Title\n\n</div>")]
    [InlineData("<details open><summary>S</summary>x</details>", "<details open>\n<summary>S</summary>\n\nx\n\n</details>")]
    [InlineData("<a href=\"https://x.example\"><img src=\"y.svg\" alt=\"Y\"></a>", "[![Y](y.svg)](https://x.example)")]
    [InlineData("<a href=\"https://x.example\" title=\"T\">x</a>", "[x](https://x.example \"T\")")]
    [InlineData("<img src=\"x.png\" width=\"100\" title=\"T\">", "![](x.png \"T\"){width=100}")]
    [InlineData("<p align=\"center\"><a href=\"x\"><img src=\"a.svg\"></a> <a href=\"y\"><img src=\"b.svg\"></a></p>",
        "<div align=\"center\">\n\n[![](a.svg)](x) [![](b.svg)](y)\n\n</div>")]
    public void Разметка_записывается_заново_а_не_копируется_из_исходника(string markdown, string expected)
    {
        CopyAll(markdown).Should().Be(expected);
    }

    [Theory]
    [InlineData("first para\n\nsecond para", "para\n\nsec", "para\n\nsec")]
    [InlineData("# Head\n\nbody text", "ad\n\nbo", "# ad\n\nbo")]
    [InlineData("- one\n- two\n- three", "two\nthr", "- two\n- thr")]
    [InlineData("| a | b |\n|---|---|\n| 1 | 2 |\n| 3 | 4 |", "2\n3", "| a | b |\n| --- | --- |\n| | 2 |\n| 3 | |")]
    [InlineData("text\n\n```\nx\ny\n```", "text\n\nx", "text\n\n```\nx\n```")]
    public void Несколько_блоков_пишутся_со_своей_разметкой(string markdown, string selected, string expected)
    {
        Copy(markdown, selected).Should().Be(expected);
    }

    [Fact]
    public void Строчный_виджет_пишется_своим_исходником()
    {
        var mention = TestWidgets.Mention();
        var blocks = ModelBuilder.Parse("hi @ivan!", ModelDump.Pipeline(mention), [mention]);
        var map = MdTextMap.Build(blocks);

        MdMarkdownWriter.Write(blocks, map, "hi @ivan!", new(0, map.Length)).Should().Be("hi @ivan!");
    }

    [Fact]
    public void Сворачиваемые_блоки_пишутся_видом_GitHub_и_тегами_details()
    {
        var blocks = new BlockVm[]
        {
            new CalloutVm("tip", [new RunVm("Title")], [new ParagraphVm([new RunVm("hidden")])]) { IsFoldable = true, IsFolded = true },
            new BlankLineVm(),
            new DetailsVm([new RunVm("More")], [new ParagraphVm([new RunVm("inside")])])
        };
        var map = MdTextMap.Build(blocks);

        MdMarkdownWriter.Write(blocks, map, string.Empty, new(0, map.Length))
            .Should().Be("> [!TIP]- Title\n> hidden\n\n<details>\n<summary>More</summary>\n\ninside\n\n</details>");
    }

    [Fact]
    public void Пустое_выделение_ничего_не_даёт()
    {
        var blocks = ModelBuilder.Parse("text");
        MdMarkdownWriter.Write(blocks, MdTextMap.Build(blocks), "text", new(1, 0)).Should().BeEmpty();
    }

    private static string CopyAll(string markdown)
    {
        var blocks = ModelBuilder.Parse(markdown);
        var map = MdTextMap.Build(blocks);
        return MdMarkdownWriter.Write(blocks, map, markdown, new(0, map.Length));
    }

    private static string Copy(string markdown, string selected)
    {
        var blocks = ModelBuilder.Parse(markdown);
        var map = MdTextMap.Build(blocks);
        var start = map.Text.IndexOf(selected, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"«{selected}» есть в тексте карты «{map.Text}»");
        return MdMarkdownWriter.Write(blocks, map, markdown, new(start, selected.Length));
    }
}