namespace MarkdigWpfViewer.Selection;

using System.Windows.Media;

/// <summary>Снимок того, что подсвечено в документе: слои пометок и поверх них выделение; включено ли выделение.</summary>
internal sealed record MdDecorationLayer(
    MdTextMap Map,
    MdTextRange Selection,
    Brush? SelectionBrush,
    IReadOnlyList<MarkSet> Marks,
    bool IsSelectionEnabled = true)
{
    /// <summary>Рисуемые пометки текста, которые задевают кусок, в порядке рисования; выделение — последним, заливкой.</summary>
    public IEnumerable<(MdTextRange Range, MdMarkKind Kind, Brush Brush)> Within(MdTextRange range)
    {
        foreach (var set in Marks)
        {
            foreach (var mark in set.Within(range))
            {
                if (mark.Brush is { } brush)
                {
                    yield return (mark.Range.Intersect(range), mark.Mark.Kind, brush);
                }
            }
        }

        if (SelectionBrush is not null)
        {
            var selected = Selection.Intersect(range);
            if (!selected.IsEmpty)
            {
                yield return (selected, MdMarkKind.Fill, SelectionBrush);
            }
        }
    }

    /// <summary>Рисуемые пометки строк этого вида, которые задевают строки с <paramref name="first"/> по <paramref name="last"/>, в порядке рисования.</summary>
    public IEnumerable<LineMark> LinesWithin(int first, int last, MdMarkKind kind)
        => Marks
            .SelectMany(set => set.Within(first, last))
            .Where(mark => mark.Mark.Kind == kind && mark.Brush is not null);

    /// <summary>Есть ли пометки этого вида.</summary>
    public bool Has(MdMarkKind kind) => Marks.Any(set => set.Has(kind));
}
