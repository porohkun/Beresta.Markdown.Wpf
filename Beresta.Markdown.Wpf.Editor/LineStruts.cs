namespace Beresta.Markdown.Wpf.Editor;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>
/// Распорки строк: в начале строки — высота над базовой линией с верхним полем, в конце — под ней с нижним;
/// у пустой строки распорка одна. Высоты и поля строки документа даёт редактор.
/// </summary>
internal sealed class LineStruts(Func<DocumentLine, (double Ascent, double Descent, double Above, double Below)?> strutOf)
    : VisualLineElementGenerator
{
    /// <inheritdoc/>
    public override int GetFirstInterestedOffset(int startOffset)
    {
        var line = CurrentContext.VisualLine.FirstDocumentLine;
        if (strutOf(line) is not { } strut)
        {
            return -1;
        }

        return startOffset <= line.Offset ? line.Offset
            : line.Length > 0 && strut.Below > 0 && startOffset <= line.EndOffset ? line.EndOffset
            : -1;
    }

    /// <inheritdoc/>
    public override VisualLineElement? ConstructElement(int offset)
    {
        var line = CurrentContext.VisualLine.FirstDocumentLine;
        if (strutOf(line) is not { } strut)
        {
            return null;
        }

        var split = line.Length > 0 && strut.Below > 0;
        return offset == line.Offset ? new StrutElement(strut.Ascent, split ? strut.Descent - strut.Below : strut.Descent)
            : split && offset == line.EndOffset ? new StrutElement(strut.Ascent - strut.Above, strut.Descent)
            : null;
    }
}
