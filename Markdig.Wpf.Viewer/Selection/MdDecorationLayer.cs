namespace MarkdigWpfViewer.Selection;

using System.Windows.Media;

/// <summary>Снимок того, что подсвечено в документе: декорации и поверх них выделение.</summary>
internal sealed record MdDecorationLayer(
    MdTextMap Map,
    MdTextRange Selection,
    Brush? SelectionBrush,
    IReadOnlyList<MdDecoration> Decorations)
{
    /// <summary>Подсветки, которые задевают кусок, в порядке рисования.</summary>
    public IEnumerable<(MdTextRange Range, Brush Brush)> Within(MdTextRange range)
    {
        foreach (var decoration in Decorations)
        {
            var common = decoration.Range.Intersect(range);
            if (!common.IsEmpty)
            {
                yield return (common, decoration.Brush);
            }
        }

        if (SelectionBrush is not null)
        {
            var selected = Selection.Intersect(range);
            if (!selected.IsEmpty)
            {
                yield return (selected, SelectionBrush);
            }
        }
    }
}