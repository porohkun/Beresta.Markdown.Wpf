namespace MarkdigWpfViewer.Scrolling;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Presenters;
using ViewModels;

/// <summary>
/// Строки исходника и вертикальные позиции во вьюере, в обе стороны. Между блоками — по их
/// позициям в панели, внутри созданного блока — по самому глубокому элементу с известными строками.
/// </summary>
internal sealed class SourcePositions
{
    private readonly MarkdownViewer _viewer;

    public SourcePositions(MarkdownViewer viewer)
    {
        _viewer = viewer;
    }

    /// <summary>Позиция строки исходника в координатах вьюера.</summary>
    public double? OffsetOf(double line)
    {
        if (_viewer.BlockPanel is not { ItemCount: > 0 } panel || _viewer.ItemsSource is not IReadOnlyList<BlockVm> items)
        {
            return null;
        }

        var index = -1;
        for (var i = 0; i < items.Count && i < panel.ItemCount; i++)
        {
            var source = items[i].Source;
            if (source.IsEmpty)
            {
                continue;
            }

            if (line < source.StartLine)
            {
                break;
            }

            index = i;
            if (line < source.EndLine + 1)
            {
                break;
            }
        }

        if (index < 0)
        {
            return ToViewer(panel, panel.GetItemTop(0));
        }

        var item = items[index].Source;
        if (line >= item.EndLine + 1)
        {
            return ToViewer(panel, panel.GetItemBottom(index));
        }

        if (Deepest(Spans(index).Where(span => span.Start <= line && line < span.End)) is { } best)
        {
            return best.Bounds.Top + (line - best.Start) / (best.End - best.Start) * best.Bounds.Height;
        }

        var top = panel.GetItemTop(index);
        var height = panel.GetItemBottom(index) - top;
        return ToViewer(panel, top + (line - item.StartLine) / (item.EndLine + 1 - item.StartLine) * height);
    }

    /// <summary>Строка исходника в позиции вьюера.</summary>
    public double? LineAt(double offset)
    {
        if (_viewer.BlockPanel is not { ItemCount: > 0 } panel || _viewer.ItemsSource is not IReadOnlyList<BlockVm> items)
        {
            return null;
        }

        var panelOffset = _viewer.TranslatePoint(new Point(0, offset), panel).Y;
        var index = Math.Clamp(panel.IndexAtOffset(panelOffset), 0, Math.Min(items.Count, panel.ItemCount) - 1);

        if (Deepest(Spans(index).Where(span => span.Bounds.Top <= offset && offset < span.Bounds.Bottom)) is { } best)
        {
            return best.Start + (offset - best.Bounds.Top) / best.Bounds.Height * (best.End - best.Start);
        }

        var source = items[index].Source;
        var top = panel.GetItemTop(index);
        var height = panel.GetItemBottom(index) - top;
        var fraction = height > 0 ? Math.Clamp((panelOffset - top) / height, 0, 1) : 0;
        return source.StartLine + fraction * (source.EndLine + 1 - source.StartLine);
    }

    private static (Rect Bounds, double Start, double End, int Depth)? Deepest(
        IEnumerable<(Rect Bounds, double Start, double End, int Depth)> spans)
    {
        (Rect Bounds, double Start, double End, int Depth)? best = null;
        foreach (var span in spans)
        {
            if (best is null || span.Depth > best.Value.Depth)
            {
                best = span;
            }
        }

        return best;
    }

    private double ToViewer(MdBlockPanel panel, double panelOffset)
        => panel.TranslatePoint(new Point(0, panelOffset), _viewer).Y;

    /// <summary>Созданные элементы блока с известными строками, с их рамками во вьюере.</summary>
    private IEnumerable<(Rect Bounds, double Start, double End, int Depth)> Spans(int index)
    {
        if (_viewer.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement container)
        {
            yield break;
        }

        var pending = new Stack<(DependencyObject Node, int Depth)>();
        pending.Push((container, 0));

        while (pending.Count > 0)
        {
            var (node, depth) = pending.Pop();
            var content = node switch
            {
                ContentPresenter presenter => presenter.Content,
                ContentControl control => control.Content,
                _ => null
            };

            if (node is FrameworkElement { ActualHeight: > 0 } element && content is NodeVm { Source.IsEmpty: false } model)
            {
                var bounds = element.TransformToAncestor(_viewer).TransformBounds(new Rect(element.RenderSize));
                yield return (bounds, model.Source.StartLine, model.Source.EndLine + 1, depth);
            }

            if (node is CodeBlockPresenter { DataContext: CodeBlockVm code } presenterOfCode
                && Visuals(presenterOfCode).OfType<MdTextBlock>().FirstOrDefault() is { ActualHeight: > 0 } text)
            {
                var bounds = text.TransformToAncestor(_viewer).TransformBounds(new Rect(text.RenderSize));
                var first = code.CodeFirstLine;
                yield return (bounds, first, first + code.Code.Split('\n').Length, depth + 1);
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                pending.Push((VisualTreeHelper.GetChild(node, i), depth + 1));
            }
        }
    }

    private static IEnumerable<DependencyObject> Visuals(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var deeper in Visuals(child))
            {
                yield return deeper;
            }
        }
    }
}
