namespace Beresta.Markdown.Wpf.Editor;

using System.Windows.Documents;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>
/// Распорка в начале строки: нулевой ширины и без текста документа, поднимает строку до высоты над и
/// под базовой линией.
/// </summary>
internal sealed class StrutElement(double ascent, double descent) : VisualLineElement(1, 0)
{
    /// <inheritdoc/>
    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        => new StrutRun(ascent, descent, TextRunProperties);

    /// <inheritdoc/>
    public override bool IsWhitespace(int visualColumn) => true;

    /// <inheritdoc/>
    public override int GetVisualColumn(int relativeTextOffset) => VisualColumn + VisualLength;

    /// <inheritdoc/>
    public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode) => -1;
}
