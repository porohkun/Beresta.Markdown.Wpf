namespace Beresta.Markdown.Wpf.Editor;

using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

/// <summary>Кусок строки со знаками цитаты пустой строки: рисует их на базовой линии строки, а высоты не занимает.</summary>
internal sealed class GapMarkerRun : TextEmbeddedObject
{
    private readonly FormattedText _text;

    public GapMarkerRun(string text, TextRunProperties properties, TextFormattingMode mode, double pixelsPerDip)
    {
        Properties = properties;
        Length = text.Length;
        _text = new(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            properties.Typeface,
            properties.FontRenderingEmSize,
            properties.ForegroundBrush,
            null,
            mode,
            pixelsPerDip);
    }

    /// <inheritdoc/>
    public override LineBreakCondition BreakBefore => LineBreakCondition.BreakRestrained;

    /// <inheritdoc/>
    public override LineBreakCondition BreakAfter => LineBreakCondition.BreakRestrained;

    /// <inheritdoc/>
    public override bool HasFixedSize => true;

    /// <inheritdoc/>
    public override CharacterBufferReference CharacterBufferReference => default;

    /// <inheritdoc/>
    public override int Length { get; }

    /// <inheritdoc/>
    public override TextRunProperties Properties { get; }

    /// <inheritdoc/>
    public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth) => new(_text.WidthIncludingTrailingWhitespace, 0, 0);

    /// <inheritdoc/>
    public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways) => new(0, -_text.Baseline, _text.WidthIncludingTrailingWhitespace, _text.Height);

    /// <inheritdoc/>
    public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
        => drawingContext.DrawText(_text, new Point(origin.X, origin.Y - _text.Baseline));
}
