namespace MarkdigWpfViewer.Presenters;

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ViewModels;

/// <summary>
/// Вертикальная панель блоков документа: создаёт только блоки, попавшие в видимую область
/// прокруток-предков, остальным отводит место по известной или оценённой высоте. Высоты помнит
/// по идентификаторам блоков, так что новый текст их не сбрасывает.
/// </summary>
public sealed class MdBlockPanel : VirtualizingPanel
{
    private const double DefaultHeight = 20;
    private const double CacheShare = 0.5;
    private const int MaxPasses = 4;

    private readonly Dictionary<HeightKey, double> _heights = [];
    private readonly List<ScrollViewer> _watched = [];
    private HeightKey[] _keys = [];
    private double[] _offsets = [0];
    private (HeightKey Key, double Top, double ViewportTop)? _anchor;

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
        UpdateKeys(owner);
        UpdateOffsets();

        var constraint = new Size(availableSize.Width, double.PositiveInfinity);
        var viewport = owner is not null && GetIsVirtualizing(owner) ? FindViewport() : null;

        (int First, int Last) range;
        var shift = 0.0;

        if (viewport is { } visible)
        {
            var moved = AnchorShift(visible);
            range = RealizeVisible(visible with { Top = visible.Top + moved, Bottom = visible.Bottom + moved }, constraint, out shift);
            shift += moved;
            RememberAnchor(visible with { Top = visible.Top + shift });
        }
        else
        {
            range = (0, _keys.Length - 1);
            Realize(range.First, range.Last, constraint);
            UpdateOffsets();
            _anchor = null;
        }

        CleanUp(range.First, range.Last);

        if (shift != 0 && viewport?.Scrolling is { } scrolling)
        {
            // BYDESIGN: сдвиг — прямо в прокрутку, мимо очереди команд ScrollViewer: явная прокрутка
            // хоста, поставленная в очередь раньше, выполнится после него и не будет перебита.
            scrolling.SetVerticalOffset(scrolling.VerticalOffset + shift);
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

        FindViewer()?.OnViewportMaybeChanged();
        return finalSize;
    }

    /// <summary>Сколько блоков у панели, созданных и нет.</summary>
    internal int ItemCount => _keys.Length;

    /// <summary>Верх блока в координатах панели — измеренный или оценённый.</summary>
    internal double GetItemTop(int index) => _offsets[Math.Clamp(index, 0, _offsets.Length - 1)];

    /// <summary>Низ блока в координатах панели.</summary>
    internal double GetItemBottom(int index) => _offsets[Math.Clamp(index + 1, 0, _offsets.Length - 1)];

    /// <summary>Блок, на который приходится позиция в координатах панели.</summary>
    internal int IndexAtOffset(double offset) => offset < 0 ? 0 : IndexAt(offset) is var index and >= 0 ? index : _keys.Length - 1;

    protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
    {
        base.OnIsItemsHostChanged(oldIsItemsHost, newIsItemsHost);

        if (newIsItemsHost && ItemsControl.GetItemsOwner(this) is MarkdownViewer viewer)
        {
            viewer.BlockPanel = this;
        }
    }

    /// <summary>Сдвигает ближайшую прокрутку так, чтобы блок оказался виден целиком или хотя бы сверху.</summary>
    protected override void BringIndexIntoView(int index)
    {
        if (index < 0 || index >= _keys.Length || FindViewport() is not { PhysicalScroller: { } scroller } viewport)
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
    }

    /// <summary>
    /// Насколько сдвинулся блок, бывший вверху видимой области в прошлый раз: так новый текст
    /// выше него не уводит видимое. Прокрутку, которую с тех пор сделал кто-то другой, не трогает.
    /// </summary>
    private double AnchorShift(Viewport viewport)
    {
        if (viewport.PhysicalScroller is null
            || _anchor is not { } anchor
            || Math.Abs(viewport.Top - anchor.ViewportTop) > 0.5)
        {
            return 0;
        }

        var index = Array.IndexOf(_keys, anchor.Key);
        return index >= 0 ? _offsets[index] - anchor.Top : 0;
    }

    private void RememberAnchor(Viewport viewport)
    {
        var index = viewport.PhysicalScroller is not null && viewport.Top > 0 ? IndexAt(viewport.Top) : -1;
        _anchor = index >= 0 ? (_keys[index], _offsets[index], viewport.Top) : null;
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
                _heights[_keys[index]] = child.DesiredSize.Height;
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

    private void UpdateKeys(ItemsControl? owner)
    {
        var count = owner?.Items.Count ?? 0;
        if (_keys.Length != count)
        {
            _keys = new HeightKey[count];
        }

        for (var i = 0; i < count; i++)
        {
            _keys[i] = HeightKey.Of(owner!.Items[i]);
        }

        if (_heights.Count > 2 * count + 64)
        {
            var current = _keys.ToHashSet();
            foreach (var key in _heights.Keys.Where(key => !current.Contains(key)).ToList())
            {
                _heights.Remove(key);
            }
        }
    }

    private void UpdateOffsets()
    {
        var known = 0;
        var total = 0.0;
        foreach (var key in _keys)
        {
            if (_heights.TryGetValue(key, out var height))
            {
                known++;
                total += height;
            }
        }

        var estimate = known > 0 ? total / known : DefaultHeight;

        _offsets = new double[_keys.Length + 1];
        for (var i = 0; i < _keys.Length; i++)
        {
            _offsets[i + 1] = _offsets[i] + (_heights.TryGetValue(_keys[i], out var height) ? height : estimate);
        }
    }

    private int IndexAt(double y)
    {
        for (var i = 0; i < _keys.Length; i++)
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
        while (last + 1 < _keys.Length && _offsets[last + 1] < bottom)
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
                viewport = scrollInfo is ScrollContentPresenter
                    ? new Viewport(bounds.Top, bounds.Bottom, scrollViewer, scrollInfo)
                    : new Viewport(bounds.Top, bounds.Bottom, null, null);
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
            FindViewer()?.OnViewportMaybeChanged();
        }
    }

    /// <summary>Вьюер, чьи блоки показывает панель, — сама или вложенная в блок.</summary>
    private MarkdownViewer? FindViewer()
        => ItemsControl.GetItemsOwner(this) as MarkdownViewer ?? Ancestors().OfType<MarkdownViewer>().FirstOrDefault();

    /// <summary>Видимая полоса панели в её координатах и попиксельная прокрутка, которую можно подвинуть.</summary>
    private readonly record struct Viewport(double Top, double Bottom, ScrollViewer? PhysicalScroller, IScrollInfo? Scrolling);

    /// <summary>Чья высота: блока по его идентификатору, а без идентификатора — самого элемента.</summary>
    private readonly record struct HeightKey(long Id, object? Item)
    {
        public static HeightKey Of(object item) => item is NodeVm { NodeId: not 0 } node ? new(node.NodeId, null) : new(0, item);
    }
}
