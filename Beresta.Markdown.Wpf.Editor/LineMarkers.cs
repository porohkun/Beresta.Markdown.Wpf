namespace Beresta.Markdown.Wpf.Editor;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>
/// Маркеры строк: длину маркера строки документа и то, пустая ли это строка цитаты, даёт редактор; знаки
/// пустой строки цитаты высоты ей не дают.
/// </summary>
internal sealed class LineMarkers(Func<DocumentLine, (int Length, bool Gap)> markerOf) : VisualLineElementGenerator
{
    /// <inheritdoc/>
    public override int GetFirstInterestedOffset(int startOffset)
    {
        var line = CurrentContext.VisualLine.FirstDocumentLine;
        return startOffset <= line.Offset && markerOf(line).Length > 0 ? line.Offset : -1;
    }

    /// <inheritdoc/>
    public override VisualLineElement? ConstructElement(int offset)
    {
        var line = CurrentContext.VisualLine.FirstDocumentLine;
        var (marker, gap) = markerOf(line);
        var length = Math.Min(marker, line.Length);
        return offset != line.Offset || length <= 0 ? null
            : gap ? new GapMarker(length, CurrentContext.Document.GetText(line.Offset, length))
            : new MarkerText(CurrentContext.VisualLine, length);
    }
}
