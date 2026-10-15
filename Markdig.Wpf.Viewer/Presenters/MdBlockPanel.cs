namespace MarkdigWpfViewer.Presenters;

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

/// <summary>
/// Вертикальная панель блоков документа: создаёт только блоки, попавшие в видимую область
/// прокруток-предков, остальным отводит место по известной или оценённой высоте.
/// </summary>
public sealed class MdBlockPanel : VirtualizingPanel
{
    private const double DefaultHeight = 20;
    private const double CacheShare = 0.5;
    private const int MaxPasses = 4;

    private readonly List<double> _heights = [];
    private readonly List<ScrollViewer> _watched = [];
    private double[] _offsets = [0];

    public MdBlockPanel()
    {
        Loaded += (_, _) => WatchScrollViewers();
        Unloaded += (_, _) => UnwatchScrollViewers();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // BYDESIGN: обращение к InternalChildren создаёт генератор контейнеров — до него
        // ItemContainerGenerator не работает.
        _ = InternalChildren;

        var owner = ItemsControl.GetItemsOwner(this);
        var count = owner?.Items.Count ?? 0;
        ResizeHeights(count);
        UpdateOffsets();

        var constraint = new Size(availableSize.Width, double.PositiveInfinity);
        var viewport = owner is not null && GetIsVirtualizing(owner) ? FindViewport() : null;

        (int First, int Last) range;
        var shift = 0.0;

        if (viewport is { } visible)
        {
            range = RealizeVisible(visible, constraint, out shift);
        }
        else
        {
            range = (0, count - 1);
            Realize(range.First, range.Last, constraint);
            UpdateOffsets();
        }

        CleanUp(range.First, range.Last);

        if (shift != 0 && viewport?.PhysicalScroller is { } scroller)
        {
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset + shift);
        }

        var width = InternalChildren
            .Cast<UIElement>()
            .Select(child => child.DesiredSize.Width)
            .DefaultIfEmpty(0)
            .Max();

        return new(width, _offsets[^1]);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var generator = ItemContainerGenerator;

        for (var i = 0; i < InternalChildren.Count; i++)
        {
            var child = InternalChildren[i];
            var index = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
            child.Arrange(new Rect(0, _offsets[index], finalSize.Width, child.DesiredSize.Height));
        }

        return finalSize;
    }

    /// <summary>Сдвигает ближайшую прокрутку так, чтобы блок оказался виден целиком или хотя бы сверху.</summary>
    protected override void BringIndexIntoView(int index)
    {
        if (index < 0 || index >= _heights.Count || FindViewport() is not { PhysicalScroller: { } scroller } viewport)
        {
            return;
        }

        var top = _offsets[index];
        var bottom = _offsets[index + 1];
        var shift = top < viewport.Top || bottom - top > viewport.Bottom - viewport.Top
            ? top - viewport.Top
            : bottom > viewport.Bottom ? bottom - viewport.Bottom : 0;

        if (shift != 0)
        {
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset + shift);
        }
    }

    protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
        base.OnItemsChanged(sender, args);

        if (args.Action is NotifyCollectionChangedAction.Remove
            or NotifyCollectionChangedAction.Replace
            or NotifyCollectionChangedAction.Move)
        {
            RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
        }

        _heights.Clear();
    }

    private (int First, int Last) RealizeVisible(Viewport viewport, Size constraint, out double shift)
    {
        shift = 0;

        var anchor = viewport.PhysicalScroller is not null && viewport.Top > 0
            ? IndexAt(viewport.Top)
            : -1;
        var anchorTop = anchor >= 0 ? _offsets[anchor] : 0;
        var cache = (viewport.Bottom - viewport.Top) * CacheShare;
        var range = (First: 0, Last: -1);

        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var next = RangeFor(viewport.Top + shift - cache, viewport.Bottom + shift + cache);
            if (pass > 0 && next.First >= range.First && next.Last <= range.Last)
            {
                break;
            }

            range = next;
            Realize(range.First, range.Last, constraint);
            UpdateOffsets();

            if (anchor >= 0)
            {
                shift = _offsets[anchor] - anchorTop;
            }
        }

        return range;
    }

    private void Realize(int first, int last, Size constraint)
    {
        if (first > last)
        {
            return;
        }

        var generator = ItemContainerGenerator;
        var start = generator.GeneratorPositionFromIndex(first);
        var childIndex = start.Offset == 0 ? start.Index : start.Index + 1;

        using (generator.StartAt(start, GeneratorDirection.Forward, true))
        {
            for (var index = first; index <= last; index++, childIndex++)
            {
                var child = (UIElement)generator.GenerateNext(out var newlyRealized);
                if (newlyRealized)
                {
                    if (childIndex >= InternalChildren.Count)
                    {
                        AddInternalChild(child);
                    }
                    else
                    {
                        InsertInternalChild(childIndex, child);
                    }

                    generator.PrepareItemContainer(child);
                }

                child.Measure(constraint);
                _heights[index] = child.DesiredSize.Height;
            }
        }
    }

    private void CleanUp(int first, int last)
    {
        var generator = ItemContainerGenerator;

        for (var i = InternalChildren.Count - 1; i >= 0; i--)
        {
            var position = new GeneratorPosition(i, 0);
            var index = generator.IndexFromGeneratorPosition(position);
            if (index >= first && index <= last)
            {
                continue;
            }

            generator.Remove(position, 1);
            RemoveInternalChildRange(i, 1);
        }
    }

    private void ResizeHeights(int count)
    {
        if (_heights.Count > count)
        {
            _heights.RemoveRange(count, _heights.Count - count);
        }

        while (_heights.Count < count)
        {
            _heights.Add(double.NaN);
        }
    }

    private void UpdateOffsets()
    {
        var known = _heights.Where(height => !double.IsNaN(height)).ToList();
        var estimate = known.Count > 0 ? known.Average() : DefaultHeight;

        _offsets = new double[_heights.Count + 1];
        for (var i = 0; i < _heights.Count; i++)
        {
            _offsets[i + 1] = _offsets[i] + (double.IsNaN(_heights[i]) ? estimate : _heights[i]);
        }
    }

    private int IndexAt(double y)
    {
        for (var i = 0; i < _heights.Count; i++)
        {
            if (_offsets[i + 1] > y)
            {
                return i;
            }
        }

        return -1;
    }

    private (int First, int Last) RangeFor(double top, double bottom)
    {
        var first = IndexAt(Math.Max(top, 0));
        if (first < 0 || bottom <= 0)
        {
            return (0, -1);
        }

        var last = first;
        while (last + 1 < _heights.Count && _offsets[last + 1] < bottom)
        {
            last++;
        }

        return (first, last);
    }

    private Viewport? FindViewport()
    {
        Viewport? viewport = null;
        IScrollInfo? scrollInfo = null;

        foreach (var node in Ancestors())
        {
            if (scrollInfo is null && node is IScrollInfo { ScrollOwner: not null } info)
            {
                scrollInfo = info;
            }

            if (node is not ScrollViewer scrollViewer)
            {
                continue;
            }

            // BYDESIGN: до первой раскладки высота прокрутки нулевая — берётся высота экрана,
            // иначе первый кадр выходит пустым.
            var height = scrollViewer.ActualHeight > 0
                ? scrollViewer.ActualHeight
                : SystemParameters.PrimaryScreenHeight;
            var bounds = scrollViewer.TransformToDescendant(this)
                .TransformBounds(new Rect(0, 0, scrollViewer.ActualWidth, height));

            if (viewport is { } current)
            {
                viewport = current with
                {
                    Top = Math.Max(current.Top, bounds.Top),
                    Bottom = Math.Min(current.Bottom, bounds.Bottom)
                };
            }
            else
            {
                var physical = scrollInfo is ScrollContentPresenter ? scrollViewer : null;
                viewport = new Viewport(bounds.Top, bounds.Bottom, physical);
            }
        }

        return viewport;
    }

    private IEnumerable<DependencyObject> Ancestors()
    {
        for (var node = VisualTreeHelper.GetParent(this); node is not null; node = VisualTreeHelper.GetParent(node))
        {
            yield return node;
        }
    }

    private void WatchScrollViewers()
    {
        UnwatchScrollViewers();

        foreach (var scrollViewer in Ancestors().OfType<ScrollViewer>())
        {
            scrollViewer.ScrollChanged += OnScrollChanged;
            _watched.Add(scrollViewer);
        }

        InvalidateMeasure();
    }

    private void UnwatchScrollViewers()
    {
        foreach (var scrollViewer in _watched)
        {
            scrollViewer.ScrollChanged -= OnScrollChanged;
        }

        _watched.Clear();
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, sender))
        {
            return;
        }

        if (e.VerticalChange != 0 || e.ViewportHeightChange != 0)
        {
            InvalidateMeasure();
        }
    }

    /// <summary>Видимая полоса панели в её координатах и прокрутка, которую можно подвинуть.</summary>
    private readonly record struct Viewport(double Top, double Bottom, ScrollViewer? PhysicalScroller);
}
