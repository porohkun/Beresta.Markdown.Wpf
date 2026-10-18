namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Media;
using Selection;

/// <summary>Строки во всю ширину документа от пометок вида <see cref="MdMarkKind.Row"/> — у созданных блоков.</summary>
public sealed class MdRowMarks : FrameworkElement
{
    private static readonly DependencyProperty LayerProperty =
        MdSelection.LayerProperty.AddOwner(
            typeof(MdRowMarks),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnLayerChanged));

    public MdRowMarks()
    {
        IsHitTestVisible = false;
    }

    /// <summary>Полосы строк в своих координатах в порядке рисования.</summary>
    internal IReadOnlyList<(Rect Row, Brush Brush)> CollectRows()
    {
        var rows = new List<(Rect Row, Brush Brush)>();
        if (TemplatedParent is not MarkdownViewer viewer
            || GetValue(LayerProperty) is not MdDecorationLayer layer
            || !layer.Has(MdMarkKind.Row)
            || viewer.RealizedLines() is not { } realized)
        {
            return rows;
        }

        foreach (var mark in layer.LinesWithin(realized.First, realized.Last, MdMarkKind.Row))
        {
            if (viewer.GetLineBand(Math.Max(mark.First, realized.First), Math.Min(mark.Last, realized.Last), this) is { } band)
            {
                rows.Add((new(0, band.Top, ActualWidth, band.Height), mark.Brush!));
            }
        }

        return rows;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        foreach (var (row, brush) in CollectRows())
        {
            drawingContext.DrawRectangle(brush, null, row);
        }
    }

    /// <summary>Сменились пометки — строки перерисовываются; одно выделение их не трогает.</summary>
    private static void OnLayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!ReferenceEquals((e.OldValue as MdDecorationLayer)?.Marks, (e.NewValue as MdDecorationLayer)?.Marks))
        {
            ((MdRowMarks)d).InvalidateVisual();
        }
    }
}
