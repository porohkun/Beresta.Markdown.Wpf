namespace Beresta.Markdown.Wpf.Editor;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>
/// Раскраска строк AvalonEdit: куски строки документа с их видами даёт редактор; в выделении у кусков нет
/// своего фона.
/// </summary>
internal sealed class SourceColorizer(
    Func<DocumentLine, IEnumerable<(int Start, int End, LookStyle Style)>> piecesOn,
    Func<IEnumerable<ISegment>> selected)
    : DocumentColorizingTransformer
{
    /// <inheritdoc/>
    protected override void ColorizeLine(DocumentLine line)
    {
        foreach (var (start, end, style) in piecesOn(line))
        {
            ChangeLinePart(start, end, style.Apply);
        }

        foreach (var segment in selected())
        {
            var start = Math.Max(segment.Offset, line.Offset);
            var end = Math.Min(segment.EndOffset, line.EndOffset);
            if (end > start)
            {
                ChangeLinePart(start, end, element => element.TextRunProperties.SetBackgroundBrush(null));
            }
        }
    }
}
