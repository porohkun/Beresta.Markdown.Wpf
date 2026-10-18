namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using Automation;

/// <summary>Картинка флажка задачи: пустой квадрат, залитый с галочкой или со знаком статуса. Не нажимается.</summary>
public sealed class MdTaskMark : FrameworkElement
{
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(MdTaskMark),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(
            nameof(Glyph),
            typeof(Geometry),
            typeof(MdTaskMark),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BrushProperty =
        DependencyProperty.Register(
            nameof(Brush),
            typeof(Brush),
            typeof(MdTaskMark),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MarkBrushProperty =
        DependencyProperty.Register(
            nameof(MarkBrush),
            typeof(Brush),
            typeof(MdTaskMark),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Задача выполнена: <c>[x]</c>.</summary>
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>Знак статуса задачи в пустом квадрате, в координатах квадрата 16 на 16; нет знака — обычный флажок.</summary>
    public Geometry? Glyph
    {
        get => (Geometry?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>Цвет флажка: рамка пустого квадрата, заливка выполненного, знак статуса.</summary>
    public Brush? Brush
    {
        get => (Brush?)GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    /// <summary>Галочка на залитом квадрате.</summary>
    public Brush? MarkBrush
    {
        get => (Brush?)GetValue(MarkBrushProperty);
        set => SetValue(MarkBrushProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 1)
        {
            return;
        }

        var corner = size / 6;
        if (!IsChecked)
        {
            drawingContext.DrawRoundedRectangle(null, new Pen(Brush, 1), new(0.5, 0.5, size - 1, size - 1), corner, corner);
            if (Glyph is { } glyph)
            {
                drawingContext.PushTransform(new ScaleTransform(size / 16, size / 16));
                drawingContext.DrawGeometry(Brush, null, glyph);
                drawingContext.Pop();
            }

            return;
        }

        drawingContext.DrawRoundedRectangle(Brush, null, new(0, 0, size, size), corner, corner);

        var mark = new StreamGeometry();
        using (var context = mark.Open())
        {
            context.BeginFigure(new(size * 0.24, size * 0.52), false, false);
            context.LineTo(new(size * 0.42, size * 0.7), true, true);
            context.LineTo(new(size * 0.76, size * 0.32), true, true);
        }

        var pen = new Pen(MarkBrush, size / 7)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        drawingContext.DrawGeometry(null, pen, mark);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MdTaskMarkAutomationPeer(this);
}
