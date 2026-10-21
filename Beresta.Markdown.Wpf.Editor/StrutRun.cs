namespace Beresta.Markdown.Wpf.Editor;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

/// <summary>Кусок строки распорки: ничего не рисует, но задаёт высоту над и под базовой линией.</summary>
internal sealed class StrutRun(double ascent, double descent, TextRunProperties properties) : TextEmbeddedObject
{
    /// <inheritdoc/>
    public override LineBreakCondition BreakBefore => LineBreakCondition.BreakRestrained;

    /// <inheritdoc/>
    public override LineBreakCondition BreakAfter => LineBreakCondition.BreakRestrained;

    /// <inheritdoc/>
    public override bool HasFixedSize => true;

    /// <inheritdoc/>
    public override CharacterBufferReference CharacterBufferReference => default;

    /// <inheritdoc/>
    public override int Length => 1;

    /// <inheritdoc/>
    public override TextRunProperties Properties => properties;

    /// <inheritdoc/>
    public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth) => new(0, ascent + descent, ascent);

    /// <inheritdoc/>
    public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways) => new(0, -ascent, 0, ascent + descent);

    /// <inheritdoc/>
    public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
    {
    }
}
