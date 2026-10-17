namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Selection;

/// <summary>Подсветка кусков текста <see cref="MdTextBlock"/>: штриховка пустых мест, пометки, выделение.</summary>
internal sealed class MdHighlightAdorner : Adorner
{
    private readonly MdTextBlock _text;

    public MdHighlightAdorner(MdTextBlock text)
        : base(text)
    {
        _text = text;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (_text.TryFindResource("Md.Gap.Brush") is Brush gap)
        {
            foreach (var rect in _text.GapRects)
            {
                drawingContext.DrawRectangle(gap, null, rect);
            }
        }

        var underline = MarkLook.UnderlineThickness(_text);
        var frame = MarkLook.FrameThickness(_text);

        foreach (var (start, end, kind, brush) in _text.Highlights)
        {
            var pen = kind == MdMarkKind.Frame ? new Pen(brush, frame) : null;
            foreach (var rect in _text.GetRangeRects(start, end))
            {
                switch (kind)
                {
                    case MdMarkKind.Underline:
                        drawingContext.DrawRectangle(brush, null, new(rect.Left, rect.Bottom - underline, rect.Width, underline));
                        break;

                    case MdMarkKind.Frame:
                        var inset = frame / 2;
                        drawingContext.DrawRectangle(
                            null,
                            pen,
                            new(rect.Left + inset, rect.Top + inset, Math.Max(0, rect.Width - frame), Math.Max(0, rect.Height - frame)));
                        break;

                    default:
                        drawingContext.DrawRectangle(brush, null, rect);
                        break;
                }
            }
        }
    }
}
