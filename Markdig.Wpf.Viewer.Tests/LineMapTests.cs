namespace MarkdigWpfViewer.Tests;

using Scrolling;
using Selection;
using ViewModels;

/// <summary>Соответствие строк двух исходников и строк исходника — кускам карты.</summary>
public sealed class LineMapTests
{
    [Fact]
    public void Без_опор_строки_совпадают()
    {
        MdLineMap.Identity.ToRight(42.5).Should().Be(42.5);
        MdLineMap.Identity.ToLeft(7).Should().Be(7);
    }

    [Fact]
    public void Между_опорами_строки_пересчитываются_линейно_в_обе_стороны()
    {
        var map = new MdLineMap([(10, 10), (20, 40)]);

        map.ToRight(15).Should().Be(25);
        map.ToLeft(25).Should().Be(15);
    }

    [Fact]
    public void За_крайними_опорами_сохраняется_их_сдвиг()
    {
        var map = new MdLineMap([(10, 15), (20, 25)]);

        map.ToRight(2).Should().Be(7);
        map.ToRight(30).Should().Be(35);
        map.ToLeft(40).Should().Be(35);
    }

    [Theory]
    [InlineData("a\nb\n\nc", 1, 1, "b")]
    [InlineData("a\nb\n\nc", 0, 3, "a b\n\nc")]
    [InlineData("```\nx\ny\nz\n```", 2, 2, "y")]
    [InlineData("| a | b |\n|---|---|\n| 1 | 2 |", 2, 2, "1\t2")]
    [InlineData("- one\n- two", 1, 1, "two")]
    public void Строки_исходника_дают_свой_кусок_карты(string markdown, int startLine, int endLine, string expected)
    {
        var map = MdTextMap.Build(ModelBuilder.Parse(markdown));

        var range = map.RangeOfSourceLines(startLine, endLine);

        range.Should().NotBeNull();
        map.GetText(range!.Value).Should().Be(expected);
    }

    [Fact]
    public void Невидимые_строки_куска_не_дают()
    {
        var map = MdTextMap.Build(ModelBuilder.Parse("a\n\n[d]: https://example.com\n\nb"));

        map.RangeOfSourceLines(2, 2).Should().BeNull();
    }
}
