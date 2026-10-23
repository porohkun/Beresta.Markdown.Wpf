namespace Beresta.Markdown.Wpf.Tests;

using Selection;

/// <summary>
/// Место исходника под позицией текстовой карты: в тексте, который исходник хранит как есть, — тот же
/// знак, иначе — начало инлайна.
/// </summary>
public sealed class SourceOffsetTests
{
    [Theory]
    [InlineData("plain text here", "text", "text here")]
    [InlineData("a **bold** b", "old", "old** b")]
    [InlineData("use `code` now", "ode", "ode` now")]
    [InlineData("use `` a`b `` now", "`b", "`b `` now")]
    [InlineData("see [the link](http://u) end", "link", "link](http://u) end")]
    [InlineData("## Head line", "line", "line")]
    [InlineData("| a | bc |\n|---|---|\n| 1 | 23 |", "3", "3 |")]
    [InlineData("> quoted words", "words", "words")]
    [InlineData("```\nfirst\nsecond\n```", "cond", "cond\n```")]
    [InlineData("- item\n\n  ```\n  code x\n  ```", "x", "x\n  ```")]
    public void В_тексте_как_в_исходнике_до_знака(string markdown, string piece, string rest)
    {
        Rest(markdown, piece).Should().StartWith(rest);
    }

    [Theory]
    [InlineData("a \\*b c", "*", "\\*b c")]
    [InlineData("x &copy; y", "©", "&copy; y")]
    [InlineData("a ![alt](i.png) b", "￼", "![alt](i.png) b")]
    public void Текст_не_как_в_исходнике_начало_инлайна(string markdown, string piece, string rest)
    {
        Rest(markdown, piece).Should().StartWith(rest);
    }

    [Fact]
    public void Конец_строки_после_последнего_знака()
    {
        Rest("first line\n\nsecond", "\n").Should().StartWith("\n\nsecond");
    }

    [Fact]
    public void Вне_блоков_ничего()
    {
        MdTextMap.Build([], string.Empty).SourceOffsetOf(0).Should().BeNull();
    }

    /// <summary>Исходник от места под первым вхождением куска в текст карты.</summary>
    private static string Rest(string markdown, string piece)
    {
        var map = MdTextMap.Build(ModelBuilder.Parse(markdown), markdown);
        var offset = map.SourceOffsetOf(map.Text.IndexOf(piece, StringComparison.Ordinal));

        offset.Should().NotBeNull();
        return markdown[offset!.Value..];
    }
}
