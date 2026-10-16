namespace MarkdigWpfViewer.Presenters;

using System.Windows.Documents;
using System.Windows.Media;

/// <summary>Подсветка кусков текста <see cref="MdTextBlock"/>: выделение и декорации.</summary>
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
        foreach (var (start, end, brush) in _text.Highlights)
        {
            foreach (var rect in _text.GetRangeRects(start, end))
            {
                drawingContext.DrawRectangle(brush, null, rect);
            }
        }
    }
}