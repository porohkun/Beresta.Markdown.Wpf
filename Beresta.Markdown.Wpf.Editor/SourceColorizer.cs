namespace Beresta.Markdown.Wpf.Editor;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>Раскраска строк AvalonEdit: куски строки документа с их видами даёт редактор.</summary>
internal sealed class SourceColorizer(Func<DocumentLine, IEnumerable<(int Start, int End, LookStyle Style)>> piecesOn)
    : DocumentColorizingTransformer
{
    /// <inheritdoc/>
    protected override void ColorizeLine(DocumentLine line)
    {
        foreach (var (start, end, style) in piecesOn(line))
        {
            ChangeLinePart(start, end, style.Apply);
        }
    }
}
