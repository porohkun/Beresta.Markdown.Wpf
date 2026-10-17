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
        // BYDESIGN: места кусков даёт раскладка текста; пока она не готова, подсветка рисуется после неё,
        // иначе осталась бы пустой.
        if (!_text.IsMeasureValid || !_text.IsArrangeValid)
        {
            _text.LayoutUpdated -= OnTextLayoutUpdated;
            _text.LayoutUpdated += OnTextLayoutUpdated;
            return;
        }

        if (_text.TryFindResource("Md.Gap.Brush") is Brush gap)
        {
            foreach (var rect in _text.GapRects)
            {
                drawingContext.DrawRectangle(gap, null, rect);
            }
        }

        var underline = MarkLook.UnderlineThickness(_text);
        var frame = MarkLook.FrameThickness(_text);
        var caret = MarkLook.CaretThickness(_text);

        foreach (var (start, end, kind, brush) in _text.Highlights)
        {
            if (kind == MdMarkKind.Caret)
            {
                if (_text.GetCaretRect(start) is { } place)
                {
                    drawingContext.DrawRectangle(brush, null, new(place.Left - caret / 2, place.Top, caret, place.Height));
                }

                continue;
            }

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

    private void OnTextLayoutUpdated(object? sender, EventArgs e)
    {
        _text.LayoutUpdated -= OnTextLayoutUpdated;
        InvalidateVisual();
    }
}
