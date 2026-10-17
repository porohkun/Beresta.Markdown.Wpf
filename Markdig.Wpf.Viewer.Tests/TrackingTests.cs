namespace MarkdigWpfViewer.Tests;

using Identity;
using Selection;

/// <summary>Отслеживаемые куски и строки: находят своё место в новом тексте или пропадают с блоком.</summary>
public sealed class TrackingTests
{
    [Fact]
    public void Кусок_находит_себя_в_том_же_тексте()
    {
        var (before, after) = Maps("first\n\nsecond words\n\nthird", "first\n\nsecond words\n\nthird");
        var tracked = before.Track(RangeOf(before, "words"));

        after.Find(tracked).Should().Be(RangeOf(after, "words"));
    }

    [Fact]
    public void Кусок_переезжает_вместе_со_своим_блоком()
    {
        var (before, after) = Maps("first\n\nsecond words", "inserted\n\nfirst\n\nsecond words");
        var tracked = before.Track(RangeOf(before, "words"));

        after.Find(tracked).Should().Be(RangeOf(after, "words"));
    }

    [Fact]
    public void Кусок_держится_за_свои_слова_при_правке_абзаца_перед_ним()
    {
        var (before, after) = Maps("Hello world again", "Oh, hello big world again");
        var tracked = before.Track(RangeOf(before, "world"));

        after.Find(tracked).Should().Be(RangeOf(after, "world"));
    }

    [Fact]
    public void Изменённый_текст_куска_остаётся_между_его_соседями()
    {
        var (before, after) = Maps("The quick brown fox jumps", "The quick red fox jumps");
        var tracked = before.Track(RangeOf(before, "brown"));

        after.Find(tracked).Should().Be(RangeOf(after, "red"));
    }

    [Fact]
    public void Удалённый_текст_куска_оставляет_пустой_кусок()
    {
        var (before, after) = Maps("Hello world again", "Hello again");
        var tracked = before.Track(RangeOf(before, "world"));

        var found = after.Find(tracked);

        found.Should().NotBeNull();
        found!.Value.IsEmpty.Should().BeTrue();
        found.Value.Start.Should().BeInRange(5, 6);
    }

    [Fact]
    public void Кусок_пропадает_вместе_со_своим_блоком()
    {
        var (before, after) = Maps("first\n\nsecond\n\nthird", "first\n\nthird");
        var tracked = before.Track(RangeOf(before, "second"));

        after.Find(tracked).Should().BeNull();
    }

    [Fact]
    public void Кусок_через_несколько_блоков_переезжает_целиком()
    {
        var (before, after) = Maps("first\n\nsecond\n\nthird", "new\n\nfirst\n\nsecond\n\nthird");
        var start = before.Text.IndexOf("cond", StringComparison.Ordinal);
        var end = before.Text.IndexOf("hir", StringComparison.Ordinal);
        var tracked = before.Track(MdTextRange.Between(start, end));

        var found = after.Find(tracked);

        found.Should().NotBeNull();
        after.GetText(found!.Value).Should().Be("cond\n\nt");
    }

    [Fact]
    public void Кусок_в_пустой_карте_не_находится()
    {
        var tracked = MdTextMap.Empty.Track(new(0, 0));

        MdTextMap.Empty.Find(tracked).Should().BeNull();
    }

    [Fact]
    public void Строки_переезжают_вместе_со_своим_блоком()
    {
        var (before, after) = Maps("a\n\nb\n\nc", "x\n\ny\n\na\n\nb\n\nc");
        var tracked = before.TrackSourceLines(2, 4);

        after.Find(tracked).Should().Be((6, 8));
    }

    [Fact]
    public void Строка_кода_держится_за_свой_текст()
    {
        var (before, after) = Maps(
            "```\nfirst\nsecond\nthird\n```",
            "```\nfirst\nadded\nsecond\nthird\n```");
        var tracked = before.TrackSourceLines(3, 3);

        after.Find(tracked).Should().Be((4, 4));
    }

    [Fact]
    public void Строки_пропадают_вместе_со_своим_блоком()
    {
        var (before, after) = Maps("first\n\nsecond\n\nthird", "first\n\nthird");
        var tracked = before.TrackSourceLines(2, 2);

        after.Find(tracked).Should().BeNull();
    }

    [Fact]
    public void Строка_вне_блоков_находится_по_своему_тексту()
    {
        var (before, after) = Maps(
            "[id]: https://example.com\n\ntext [link][id]",
            "[id]: https://example.com\n\nmore\n\ntext [link][id]");
        var tracked = before.TrackSourceLines(0, 0);

        after.Find(tracked).Should().Be((0, 0));
    }

    private static (MdTextMap Before, MdTextMap After) Maps(string before, string after)
    {
        var identity = new ModelIdentity();
        var first = MdTextMap.Build(ModelBuilder.Parse(before, identity: identity), before);
        var second = MdTextMap.Build(ModelBuilder.Parse(after, identity: identity), after);
        return (first, second);
    }

    private static MdTextRange RangeOf(MdTextMap map, string text)
        => new(map.Text.IndexOf(text, StringComparison.Ordinal), text.Length);
}
