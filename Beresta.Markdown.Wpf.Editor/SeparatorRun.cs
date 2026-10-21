namespace Beresta.Markdown.Wpf.Editor;

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

/// <summary>Кусок строки разделителя столбцов: место до знака, сам знак и место после.</summary>
internal sealed class SeparatorRun(string glyph, double before, double after, TextRunProperties properties, TextFormattingMode mode, double pixelsPerDip)
    : TextEmbeddedObject
{
    private FormattedText? _text;

    /// <inheritdoc/>
    public override LineBreakCondition BreakBefore => LineBreakCondition.BreakPossible;

    /// <inheritdoc/>
    public override LineBreakCondition BreakAfter => LineBreakCondition.BreakPossible;

    /// <inheritdoc/>
    public override bool HasFixedSize => true;

    /// <inheritdoc/>
    public override CharacterBufferReference CharacterBufferReference => default;

    /// <inheritdoc/>
    public override int Length => 1;

    /// <inheritdoc/>
    public override TextRunProperties Properties => properties;

    private FormattedText Text => _text ??= new(
        glyph,
        CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        properties.Typeface,
        properties.FontRenderingEmSize,
        properties.ForegroundBrush,
        null,
        mode,
        pixelsPerDip);

    private double Width => before + Text.WidthIncludingTrailingWhitespace + after;

    /// <inheritdoc/>
    public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth) => new(Width, Text.Height, Text.Baseline);

    /// <inheritdoc/>
    public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways) => new(0, -Text.Baseline, Width, Text.Height);

    /// <inheritdoc/>
    public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
        => drawingContext.DrawText(Text, new(origin.X + before, origin.Y - Text.Baseline));
}
