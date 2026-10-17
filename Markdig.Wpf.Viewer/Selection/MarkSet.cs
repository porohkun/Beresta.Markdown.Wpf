namespace MarkdigWpfViewer.Selection;

using System.Windows.Media;

/// <summary>
/// Пометки слоя, разложенные по карте для быстрого отбора: пометки текста — кусками карты, пометки
/// строк — строками исходника; и те и другие по началу, с самым дальним концом среди предыдущих.
/// Черты — отдельно, по месту.
/// </summary>
internal sealed class MarkSet
{
    private readonly TextMark[] _text;
    private readonly int[] _textReach;
    private readonly LineMark[] _lines;
    private readonly int[] _lineReach;
    private readonly TextMark[] _carets;
    private readonly HashSet<MdMarkKind> _kinds;

    /// <param name="brushOf">Кисть темы по имени оформления для вида пометки.</param>
    public MarkSet(MdMarkLayer layer, MdTextMap map, Func<string, MdMarkKind, Brush?> brushOf)
    {
        Layer = layer;
        var text = new List<TextMark>();
        var lines = new List<LineMark>();
        var carets = new List<TextMark>();

        foreach (var mark in layer.Marks)
        {
            var brush = mark.Brush ?? (mark.Style is { } style ? brushOf(style, mark.Kind) : null);
            if (mark.Kind is MdMarkKind.Row or MdMarkKind.Bar or MdMarkKind.Icon)
            {
                if (LinesOf(mark, map) is { } span)
                {
                    lines.Add(new(mark, span.First, span.Last, brush));
                }
            }
            else if (mark.Kind == MdMarkKind.Caret)
            {
                if (RangeOf(mark, map) is { } place)
                {
                    carets.Add(new(mark, new(place.Start, 0), brush));
                }
            }
            else if (RangeOf(mark, map) is { IsEmpty: false } range)
            {
                text.Add(new(mark, range, brush));
            }
        }

        _text = text.OrderBy(mark => mark.Range.Start).ToArray();
        _textReach = Reach(_text.Select(mark => mark.Range.End));
        _lines = lines.OrderBy(mark => mark.First).ToArray();
        _lineReach = Reach(_lines.Select(mark => mark.Last));
        _carets = carets.OrderBy(mark => mark.Range.Start).ToArray();
        _kinds = text
            .Concat(carets)
            .Where(mark => mark.Brush is not null)
            .Select(mark => mark.Mark.Kind)
            .Concat(lines.Where(mark => mark.Brush is not null).Select(mark => mark.Mark.Kind))
            .ToHashSet();
    }

    public MdMarkLayer Layer { get; }

    /// <summary>Есть ли рисуемые пометки этого вида.</summary>
    public bool Has(MdMarkKind kind) => _kinds.Contains(kind);

    /// <summary>Пометки текста, которые задевают кусок, по началу.</summary>
    public IEnumerable<TextMark> Within(MdTextRange range)
    {
        var to = FirstAtOrAfter(_text.Length, i => _text[i].Range.Start >= range.End);
        for (var i = FirstAtOrAfter(to, i => _textReach[i] > range.Start); i < to; i++)
        {
            if (_text[i].Range.End > range.Start)
            {
                yield return _text[i];
            }
        }
    }

    /// <summary>Черты, которые стоят в куске или на его краях, по месту.</summary>
    public IEnumerable<TextMark> CaretsWithin(MdTextRange range)
    {
        for (var i = FirstAtOrAfter(_carets.Length, i => _carets[i].Range.Start >= range.Start);
             i < _carets.Length && _carets[i].Range.Start <= range.End;
             i++)
        {
            yield return _carets[i];
        }
    }

    /// <summary>Пометки строк, которые задевают строки с <paramref name="first"/> по <paramref name="last"/>, по началу.</summary>
    public IEnumerable<LineMark> Within(int first, int last)
    {
        var to = FirstAtOrAfter(_lines.Length, i => _lines[i].First > last);
        for (var i = FirstAtOrAfter(to, i => _lineReach[i] >= first); i < to; i++)
        {
            if (_lines[i].Last >= first)
            {
                yield return _lines[i];
            }
        }
    }

    private static MdTextRange? RangeOf(MdMark mark, MdTextMap map)
        => mark.Range ?? (mark.Lines is { } lines ? map.RangeOfSourceLines(lines.Start, lines.End) : null);

    private static (int First, int Last)? LinesOf(MdMark mark, MdTextMap map)
    {
        if (mark.Lines is { } lines)
        {
            return lines;
        }

        if (mark.Range is not { } range
            || map.SourceLineOf(range.Start) is not { } first
            || map.SourceLineOf(range.IsEmpty ? range.Start : range.End - 1) is not { } last)
        {
            return null;
        }

        return (first, Math.Max(first, last));
    }

    /// <summary>Самый дальний конец среди пометок до каждой включительно.</summary>
    private static int[] Reach(IEnumerable<int> ends)
    {
        var reach = new List<int>();
        var farthest = int.MinValue;
        foreach (var end in ends)
        {
            farthest = Math.Max(farthest, end);
            reach.Add(farthest);
        }

        return reach.ToArray();
    }

    /// <summary>Первый номер среди <paramref name="count"/>, для которого условие выполнено; условие монотонно.</summary>
    private static int FirstAtOrAfter(int count, Func<int, bool> holds)
    {
        var low = 0;
        var high = count;
        while (low < high)
        {
            var middle = (low + high) / 2;
            if (holds(middle))
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        return low;
    }
}
