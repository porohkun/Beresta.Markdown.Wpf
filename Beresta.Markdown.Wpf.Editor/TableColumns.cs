namespace Beresta.Markdown.Wpf.Editor;

using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>Столбцы таблиц: разделители строки документа с местом до и после них даёт редактор.</summary>
internal sealed class TableColumns(Func<DocumentLine, IReadOnlyList<(int Offset, double Before, double After)>> separatorsOf)
    : VisualLineElementGenerator
{
    /// <inheritdoc/>
    public override int GetFirstInterestedOffset(int startOffset)
    {
        foreach (var (offset, _, _) in separatorsOf(CurrentContext.VisualLine.FirstDocumentLine))
        {
            if (offset >= startOffset)
            {
                return offset;
            }
        }

        return -1;
    }

    /// <inheritdoc/>
    public override VisualLineElement? ConstructElement(int offset)
    {
        foreach (var (at, before, after) in separatorsOf(CurrentContext.VisualLine.FirstDocumentLine))
        {
            if (at == offset)
            {
                var view = CurrentContext.TextView;
                var glyph = CurrentContext.Document.GetCharAt(offset) is '\t' ? new string(' ', CurrentContext.TextView.Options.IndentationSize) : CurrentContext.Document.GetText(offset, 1);
                return new SeparatorElement(glyph, before, after, TextOptions.GetTextFormattingMode(view), VisualTreeHelper.GetDpi(view).PixelsPerDip);
            }
        }

        return null;
    }
}
