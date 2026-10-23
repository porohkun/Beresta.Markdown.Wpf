namespace Beresta.Markdown.Wpf.Editor;

using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>
/// Разделитель столбцов — <c>|</c> таблицы, запятая CSV — с местом до и после него: столбцы встают ровно,
/// а место не текст — каретка его перескакивает, в выделение и копирование оно не входит.
/// </summary>
internal sealed class SeparatorElement(string glyph, double before, double after, TextFormattingMode mode, double pixelsPerDip)
    : VisualLineElement(1, 1)
{
    /// <inheritdoc/>
    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        => new SeparatorRun(glyph, before, after, TextRunProperties, mode, pixelsPerDip);
}
