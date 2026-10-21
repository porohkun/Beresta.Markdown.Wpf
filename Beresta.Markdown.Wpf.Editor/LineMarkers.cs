namespace Beresta.Markdown.Wpf.Editor;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>Маркеры строк: длину маркера строки документа даёт редактор.</summary>
internal sealed class LineMarkers(Func<DocumentLine, int> markerOf) : VisualLineElementGenerator
{
    /// <inheritdoc/>
    public override int GetFirstInterestedOffset(int startOffset)
    {
        var line = CurrentContext.VisualLine.FirstDocumentLine;
        return startOffset <= line.Offset && markerOf(line) > 0 ? line.Offset : -1;
    }

    /// <inheritdoc/>
    public override VisualLineElement? ConstructElement(int offset)
    {
        var line = CurrentContext.VisualLine.FirstDocumentLine;
        var length = Math.Min(markerOf(line), line.Length);
        return offset == line.Offset && length > 0 ? new MarkerText(CurrentContext.VisualLine, length) : null;
    }
}
