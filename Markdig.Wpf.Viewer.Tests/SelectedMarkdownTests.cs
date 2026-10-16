namespace MarkdigWpfViewer.Tests;

using Selection;

/// <summary>Разметка выделенного: выделенный кусок, записанный markdown со своим оформлением.</summary>
public sealed class SelectedMarkdownTests
{
    [Theory]
    [InlineData("Абзац с **жирным словом** и [ссылкой](https://example.com).", "с жирным сл", "с **жирным сл**")]
    [InlineData("see [the docs](https://x.example/a) now", "the do", "[the do](https://x.example/a)")]
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
    [InlineData("```cs\r\nx\r\ny\r\n```\r\n\r\ntext\r\nmore", "```cs\nx\ny\n```\n\ntext\nmore")]
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
