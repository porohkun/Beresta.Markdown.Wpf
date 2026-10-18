namespace Beresta.Markdown.Wpf.Selection;

/// <summary>Кусок текстовой карты: <see cref="Length"/> символов с <see cref="Start"/>.</summary>
public readonly record struct MdTextRange(int Start, int Length)
{
    /// <summary>Позиция сразу за куском.</summary>
    public int End => Start + Length;

    public bool IsEmpty => Length <= 0;

    /// <summary>Кусок от <paramref name="start"/> до <paramref name="end"/> в любом порядке.</summary>
    public static MdTextRange Between(int start, int end)
        => new(Math.Min(start, end), Math.Abs(end - start));

    /// <summary>Общая часть двух кусков; не пересекаются — пустой кусок.</summary>
    public MdTextRange Intersect(MdTextRange other)
    {
        var start = Math.Max(Start, other.Start);
        var end = Math.Min(End, other.End);
        return end > start ? new(start, end - start) : new(start, 0);
    }
}