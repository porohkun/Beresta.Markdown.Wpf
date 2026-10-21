namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;
using Automation;
using Selection;
using ViewModels;

/// <summary>
/// The picture of a task box: an empty square, a filled one with a check mark, or one with a status
/// sign. Pressed when <see cref="MarkdownViewer.AreControlsEditable"/> is on.
/// </summary>
public sealed class MdTaskMark : FrameworkElement
{
    private static readonly DependencyProperty EditableProperty =
        MarkdownViewer.AreControlsEditableProperty.AddOwner(
            typeof(MdTaskMark),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnEditableChanged));

    /// <summary>The task is done: <c>[x]</c>.</summary>
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(MdTaskMark),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// The status sign of the task in an empty square, in the coordinates of a 16 by 16 square;
    /// none — a plain box.
    /// </summary>
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(
            nameof(Glyph),
            typeof(Geometry),
            typeof(MdTaskMark),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The color of the box: the frame of an empty square, the fill of a done one, the status sign.</summary>
    public static readonly DependencyProperty BrushProperty =
        DependencyProperty.Register(
            nameof(Brush),
            typeof(Brush),
            typeof(MdTaskMark),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The check mark on a filled square.</summary>
    public static readonly DependencyProperty MarkBrushProperty =
        DependencyProperty.Register(
            nameof(MarkBrush),
            typeof(Brush),
            typeof(MdTaskMark),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <inheritdoc cref="IsCheckedProperty"/>
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <inheritdoc cref="GlyphProperty"/>
    public Geometry? Glyph
    {
        get => (Geometry?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <inheritdoc cref="BrushProperty"/>
    public Brush? Brush
    {
        get => (Brush?)GetValue(BrushProperty);
        set => SetValue(BrushProperty, value);
    }

    /// <inheritdoc cref="MarkBrushProperty"/>
    public Brush? MarkBrush
    {
        get => (Brush?)GetValue(MarkBrushProperty);
        set => SetValue(MarkBrushProperty, value);
    }

    /// <inheritdoc/>
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

    /// <summary>Задачу можно нажать: контролы вьюера правят исходник.</summary>
    internal bool IsEditable => (bool)GetValue(EditableProperty);

    /// <summary>Нажатие: вьюер спрашивает, что записать в исходник; задачу нельзя нажать — <c>false</c>.</summary>
    internal bool Press()
    {
        if (!IsEditable || DataContext is not TaskVm task || !MarkdownViewer.ActivateWidgetCommand.CanExecute(task, this))
        {
            return false;
        }

        MarkdownViewer.ActivateWidgetCommand.Execute(task, this);
        return true;
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new MdTaskMarkAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (IsEditable)
        {
            e.Handled = CaptureMouse();
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!IsMouseCaptured)
        {
            return;
        }

        ReleaseMouseCapture();
        e.Handled = true;
        if (IsMouseOver)
        {
            Press();
        }
    }

    private static void OnEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var mark = (MdTaskMark)d;
        var editable = (bool)e.NewValue;
        MdSelection.SetIsInteractive(mark, editable);
        mark.Cursor = editable ? Cursors.Hand : null;
    }
}
