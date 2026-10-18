namespace Beresta.Markdown.Wpf.Selection;

/// <summary>A piece of the text map: <see cref="Length"/> characters from <see cref="Start"/>.</summary>
public readonly record struct MdTextRange(int Start, int Length)
{
    /// <summary>The position right after the piece.</summary>
    public int End => Start + Length;

    /// <summary>The piece has no characters.</summary>
    public bool IsEmpty => Length <= 0;

    /// <summary>The piece from <paramref name="start"/> to <paramref name="end"/> in any order.</summary>
    public static MdTextRange Between(int start, int end)
        => new(Math.Min(start, end), Math.Abs(end - start));

    /// <summary>The common part of two pieces; they do not intersect — an empty piece.</summary>
    public MdTextRange Intersect(MdTextRange other)
    {
        var start = Math.Max(Start, other.Start);
        var end = Math.Min(End, other.End);
        return end > start ? new(start, end - start) : new(start, 0);
    }
}