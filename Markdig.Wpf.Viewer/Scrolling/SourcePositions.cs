namespace MarkdigWpfViewer.Scrolling;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Presenters;
using ViewModels;

/// <summary>
/// Строки исходника и вертикальные позиции во вьюере, в обе стороны. Между блоками — по их
/// позициям в панели, внутри созданного блока — по самому глубокому элементу с известными строками.
/// Внутри элемента строки делят его высоту поровну, а пустое место между ними стоит целиком перед
/// своей строкой.
/// </summary>
internal sealed class SourcePositions
{
    private readonly MarkdownViewer _viewer;

    public SourcePositions(MarkdownViewer viewer)
    {
        _viewer = viewer;
    }

    /// <summary>Позиция строки исходника в координатах вьюера.</summary>
    public double? OffsetOf(double line) => OffsetsOf([line])[0];

    /// <summary>
    /// Позиции строк исходника в координатах вьюера за один проход по блокам; строки — по
    /// возрастанию.
    /// </summary>
    public double?[] OffsetsOf(IReadOnlyList<double> lines)
    {
        var offsets = new double?[lines.Count];
        if (_viewer.BlockPanel is not { ItemCount: > 0 } panel || _viewer.ItemsSource is not IReadOnlyList<BlockVm> items)
        {
            return offsets;
        }

        var count = Math.Min(items.Count, panel.ItemCount);
        var spans = new Dictionary<int, List<(Rect Bounds, double Start, double End, int Depth)>>();
        var next = 0;
        var index = -1;

        for (var k = 0; k < lines.Count; k++)
        {
            var line = lines[k];
            for (; next < count; next++)
            {
                var source = items[next].Source;
                if (source.IsEmpty)
                {
                    continue;
                }

                if (line < source.StartLine)
                {
                    break;
                }

                index = next;
                if (line < source.EndLine + 1)
                {
                    break;
                }
            }

            offsets[k] = OffsetIn(panel, items, index, line, spans);
        }

        return offsets;
    }

    /// <summary>Строка исходника в позиции вьюера.</summary>
    public double? LineAt(double offset)
    {
        if (_viewer.BlockPanel is not { ItemCount: > 0 } panel || _viewer.ItemsSource is not IReadOnlyList<BlockVm> items)
        {
            return null;
        }

        var panelOffset = _viewer.TranslatePoint(new(0, offset), panel).Y;
        var index = Math.Clamp(panel.IndexAtOffset(panelOffset), 0, Math.Min(items.Count, panel.ItemCount) - 1);

        var gaps = Gaps();
        if (Deepest(Spans(index).Where(span => span.Bounds.Top <= offset && offset < span.Bounds.Bottom)) is { } best)
        {
            return LineIn(best.Bounds.Top, best.Bounds.Height, best.Start, best.End, offset, gaps);
        }

        var source = items[index].Source;
        var top = panel.GetItemTop(index);
        var height = panel.GetItemBottom(index) - top;
        return LineIn(top, height, source.StartLine, source.EndLine + 1, Math.Clamp(panelOffset, top, top + height), gaps);
    }

    /// <summary>Позиция строки в блоке <paramref name="index"/>; созданные элементы блока берутся один раз на проход.</summary>
    private double? OffsetIn(
        MdBlockPanel panel,
        IReadOnlyList<BlockVm> items,
        int index,
        double line,
        Dictionary<int, List<(Rect Bounds, double Start, double End, int Depth)>> spans)
    {
        if (index < 0)
        {
            return ToViewer(panel, panel.GetItemTop(0));
        }

        var item = items[index].Source;
        if (line >= item.EndLine + 1)
        {
            return ToViewer(panel, panel.GetItemBottom(index));
        }

        if (!spans.TryGetValue(index, out var known))
        {
            spans[index] = known = Spans(index).ToList();
        }

        var gaps = Gaps();
        if (Deepest(known.Where(span => span.Start <= line && line < span.End)) is { } best)
        {
            return OffsetIn(best.Bounds.Top, best.Bounds.Height, best.Start, best.End, line, gaps);
        }

        var top = panel.GetItemTop(index);
        var height = panel.GetItemBottom(index) - top;
        return ToViewer(panel, OffsetIn(top, height, item.StartLine, item.EndLine + 1, line, gaps));
    }

    /// <summary>
    /// Позиция строки внутри элемента со строками с <paramref name="start"/> до <paramref name="end"/>:
    /// пустые места внутри стоят целиком перед своими строками, остальную высоту строки делят поровну.
    /// </summary>
    private static double OffsetIn(double top, double height, double start, double end, double line, IReadOnlyList<MdGap> gaps)
    {
        var inner = 0.0;
        var above = 0.0;
        foreach (var gap in gaps.Where(gap => gap.Line > start && gap.Line < end))
        {
            inner += gap.Height;
            if (gap.Line <= line)
            {
                above += gap.Height;
            }
        }

        var share = Math.Max(0, height - inner) / (end - start);
        return top + (line - start) * share + above;
    }

    /// <summary>Строка в позиции внутри элемента — обратное к <see cref="OffsetIn(double, double, double, double, double, IReadOnlyList{MdGap})"/>; в пустом месте — строка за ним.</summary>
    private static double LineIn(double top, double height, double start, double end, double offset, IReadOnlyList<MdGap> gaps)
    {
        var inner = gaps.Where(gap => gap.Line > start && gap.Line < end).ToList();
        var share = Math.Max(0, height - inner.Sum(gap => gap.Height)) / (end - start);
        var position = top;
        var line = start;

        foreach (var gap in inner)
        {
            var before = (gap.Line - line) * share;
            if (offset < position + before || share == 0)
            {
                break;
            }

            position += before;
            line = gap.Line;
            if (offset < position + gap.Height)
            {
                return line;
            }

            position += gap.Height;
        }

        return share > 0 ? Math.Min(end, line + (offset - position) / share) : line;
    }

    /// <summary>Пустые места вьюера по строкам.</summary>
    private IReadOnlyList<MdGap> Gaps()
        => (_viewer.GetValue(GapLayout.LayoutProperty) as GapLayout)?.Gaps ?? [];

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
        => panel.TranslatePoint(new(0, panelOffset), _viewer).Y;

    /// <summary>
    /// Созданные элементы блока с известными строками, с их рамками во вьюере. Текст отдаёт полосу
    /// каждой своей строки исходника — от её экранной строки до экранной строки следующей.
    /// </summary>
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

            if (node is FrameworkElement { ActualHeight: > 0 } element
                && content is NodeVm { Source.IsEmpty: false } model and not InlineVm)
            {
                var bounds = element.TransformToAncestor(_viewer).TransformBounds(new(element.RenderSize));
                yield return (bounds, model.Source.StartLine, model.Source.EndLine + 1, depth);
            }

            if (node is MdTextBlock { ActualHeight: > 0 } textBlock && textBlock.SourceRows is { Count: > 0 } rows)
            {
                var toViewer = textBlock.TransformToAncestor(_viewer);
                for (var i = 0; i < rows.Count; i++)
                {
                    var top = rows[i].Row.Top;
                    var bottom = i + 1 < rows.Count ? rows[i + 1].Row.Top : textBlock.ActualHeight;
                    if (bottom > top)
                    {
                        var end = i + 1 < rows.Count ? rows[i + 1].Line : rows[i].Line + 1;
                        yield return (toViewer.TransformBounds(new(0, top, textBlock.ActualWidth, bottom - top)), rows[i].Line, end, depth + 1);
                    }
                }
            }

            if (node is CodeBlockPresenter { ActualHeight: > 0 } code && code.SourceRows().ToList() is { Count: > 0 } codeRows)
            {
                var toViewer = code.TransformToAncestor(_viewer);
                for (var i = 0; i < codeRows.Count; i++)
                {
                    var top = codeRows[i].Row.Top;
                    var bottom = i + 1 < codeRows.Count ? codeRows[i + 1].Row.Top : codeRows[i].Row.Bottom;
                    if (bottom > top)
                    {
                        yield return (toViewer.TransformBounds(new(0, top, code.ActualWidth, bottom - top)), codeRows[i].Line, codeRows[i].Line + 1, depth + 1);
                    }
                }

                if (code.DataContext is CodeBlockVm { IsFenced: true } fenced && codeRows[^1].Line < fenced.Source.EndLine)
                {
                    var fenceTop = codeRows[^1].Row.Bottom;
                    var fence = new Rect(0, fenceTop, code.ActualWidth, Math.Max(0, code.ActualHeight - fenceTop));
                    yield return (toViewer.TransformBounds(fence), fenced.Source.EndLine, fenced.Source.EndLine + 1, depth + 1);
                }
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                pending.Push((VisualTreeHelper.GetChild(node, i), depth + 1));
            }
        }
    }
}