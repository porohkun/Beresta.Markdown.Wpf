namespace Beresta.Markdown.Wpf.Tests;

using Selection;

/// <summary>
/// Позиция текстовой карты на месте исходника, обратная месту исходника под позицией: в тексте, который
/// исходник хранит как есть, — перед тем же знаком, иначе — начало инлайна; места не видно — ничего.
/// </summary>
public sealed class PositionOfSourceTests
{
    [Theory]
    [InlineData("plain text here", "text", "text here")]
    [InlineData("a **bold** b", "old", "old b")]
    [InlineData("use `code` now", "ode", "ode now")]
    [InlineData("see [the link](http://u) end", "link", "link end")]
    [InlineData("## Head line", "line", "line")]
    [InlineData("| a | bc |\n|---|---|\n| 1 | 23 |", "3 |", "3")]
    [InlineData("> quoted words", "words", "words")]
    [InlineData("```\nfirst\nsecond\n```", "cond", "cond")]
    [InlineData("- item\n\n  ```\n  code x\n  ```", "x\n", "x")]
    public void В_тексте_как_в_исходнике_перед_тем_же_знаком(string markdown, string piece, string rest)
    {
        var map = Map(markdown);

        var position = map.PositionOfSource(markdown.IndexOf(piece, StringComparison.Ordinal));

        position.Should().NotBeNull();
        map.Text[position!.Value..].Should().StartWith(rest);
    }

    [Theory]
    [InlineData("a \\*b c", "\\*", "*b c")]
    [InlineData("x &copy; y", "&copy;", "© y")]
    public void Текст_не_как_в_исходнике_начало_инлайна(string markdown, string piece, string rest)
    {
        var map = Map(markdown);

        map.Text[map.PositionOfSource(markdown.IndexOf(piece, StringComparison.Ordinal) + 1)!.Value..].Should().StartWith(rest);
    }

    [Theory]
    [InlineData("see [the link](http://u) end", "http")]
    [InlineData("```cs\ncode\n```", "cs")]
    [InlineData("text\n\n---\n\nmore", "---")]
    public void Места_которое_не_нарисовано_нет(string markdown, string piece)
        => Map(markdown).PositionOfSource(markdown.IndexOf(piece, StringComparison.Ordinal)).Should().BeNull();

    [Theory]
    [InlineData("plain text here")]
    [InlineData("a **bold** and `code` b")]
    [InlineData("| a | bc |\n|---|---|\n| 1 | 23 |")]
    [InlineData("```\nfirst\nsecond\n```")]
    public void Обратно_месту_исходника_под_позицией(string markdown)
    {
        var map = Map(markdown);

        for (var position = 0; position < map.Text.Length; position++)
        {
            if (map.Text[position] != '\n' && map.SourceOffsetOf(position) is { } offset)
            {
                map.PositionOfSource(offset).Should().Be(position, $"at {position}");
            }
        }
    }

    private static MdTextMap Map(string markdown) => MdTextMap.Build(ModelBuilder.Parse(markdown), markdown);
}
