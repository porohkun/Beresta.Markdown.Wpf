namespace Beresta.Markdown.Wpf.Editor;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>Распорки строк: высоту над и под базовой линией строки документа даёт редактор.</summary>
internal sealed class LineStruts(Func<DocumentLine, (double Ascent, double Descent)?> strutOf) : VisualLineElementGenerator
{
    /// <inheritdoc/>
    public override int GetFirstInterestedOffset(int startOffset)
    {
        var line = CurrentContext.VisualLine.FirstDocumentLine;
        return startOffset <= line.Offset && strutOf(line) is not null ? line.Offset : -1;
    }

    /// <inheritdoc/>
    public override VisualLineElement? ConstructElement(int offset)
    {
        var line = CurrentContext.VisualLine.FirstDocumentLine;
        return offset == line.Offset && strutOf(line) is { } strut ? new StrutElement(strut.Ascent, strut.Descent) : null;
    }
}
