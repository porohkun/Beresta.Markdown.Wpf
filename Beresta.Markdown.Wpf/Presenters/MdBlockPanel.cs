namespace Beresta.Markdown.Wpf.Presenters;

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Scrolling;
using ViewModels;

/// <summary>
/// The vertical panel of the blocks of a document: creates only the blocks that fall into the
/// visible area of the scrolling ancestors and gives the rest room by a known or estimated height;
/// the element a derived viewer puts in place of blocks, and the block it stands in, are created
/// wherever they stand. Heights are remembered by the ids of blocks, so a new text keeps them.
/// </summary>
public sealed class MdBlockPanel : VirtualizingPanel
{
    private const double DefaultHeight = 20;
    private const double CacheShare = 0.5;
    private const int MaxPasses = 4;

    private static readonly DependencyProperty GapsProperty =
        GapLayout.LayoutProperty.AddOwner(
            typeof(MdBlockPanel),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnGapsChanged));

    private readonly Dictionary<HeightKey, double> _heights = [];
    private readonly Dictionary<Type, int> _kindOf = [];
    private readonly List<(double Sum, int Count)> _kindHeights = [];
    private readonly List<ScrollViewer> _watched = [];
    private readonly List<(UIElement Child, Rect Rect)> _arranged = [];
    private HeightKey[] _keys = [];
    private int[] _kinds = [];
    private double[] _before = [];
    private double[] _after = [];
    private double[] _inside = [];
    private double[] _measured = [];
    private double[] _offsets = [0];
    private double[] _tops = [];
    private double _measuredSum;
    private int _measuredCount;
    private double[] _estimates = [];
    private int _changedFrom;
    private bool _keysChanged = true;
    private bool _hasGaps;
    private int _pinned = -1;
    private int _windowVersion = -1;
    private bool _hold;
    private (HeightKey Key, int Index, double Top, double ViewportTop)? _anchor;

    /// <summary>Creates an empty panel.</summary>
    public MdBlockPanel()
    {
        Loaded += (_, _) => WatchScrollViewers();
        Unloaded += (_, _) => UnwatchScrollViewers();
    }

    /// <summary>Сколько блоков у панели, созданных и нет.</summary>
    internal int ItemCount => _keys.Length;

    /// <summary>Верх блока в координатах панели — измеренный или оценённый, под пустым местом перед ним.</summary>
    internal double GetItemTop(int index)
        => _tops.Length == 0 ? 0 : _tops[Math.Clamp(index, 0, _tops.Length - 1)];

    /// <summary>Низ блока в координатах панели, над пустым местом после него.</summary>
    internal double GetItemBottom(int index)
    {
        if (_tops.Length == 0)
        {
            return 0;
        }

        var clamped = Math.Clamp(index, 0, _tops.Length - 1);
        return _offsets[clamped + 1] - _after[clamped];
    }

    /// <summary>Блок, на который приходится позиция в координатах панели.</summary>
    internal int IndexAtOffset(double offset) => offset < 0 ? 0 : IndexAt(offset) is var index and >= 0 ? index : _keys.Length - 1;

    /// <summary>
    /// Номера первого и последнего созданных блоков у видимой области; окно, созданное вдали от
    /// них, не в счёт. Созданных нет — <c>null</c>.
    /// </summary>
    internal (int First, int Last)? RealizedRange
    {
        get
        {
            var count = InternalChildren.Count;
            if (count == 0)
            {
                return null;
            }

            var generator = ItemContainerGenerator;
            var first = generator.IndexFromGeneratorPosition(new(0, 0));
            var last = generator.IndexFromGeneratorPosition(new(count - 1, 0));
            if (count > 1 && first == _pinned && generator.IndexFromGeneratorPosition(new(1, 0)) != first + 1)
            {
                first = generator.IndexFromGeneratorPosition(new(1, 0));
            }
            else if (count > 1 && last == _pinned && generator.IndexFromGeneratorPosition(new(count - 2, 0)) != last - 1)
            {
                last = generator.IndexFromGeneratorPosition(new(count - 2, 0));
            }

            return first < 0 || last < first ? null : (first, last);
        }
    }

    /// <summary>
    /// Держит строку окна: окно стоит там, где оно на экране сейчас, пока прокрутку не сдвинет кто-то
    /// другой. Раскладка ещё не готова — окно встаёт на якорь после следующей.
    /// </summary>
    internal void HoldWindow()
    {
        _hold = true;
        if (!_keysChanged
            && _pinned >= 0
            && _pinned < _tops.Length
            && FindViewport() is { PhysicalScroller: not null } viewport)
        {
            _anchor = (_keys[_pinned], _pinned, _tops[_pinned], viewport.Top);
        }

        InvalidateMeasure();
    }

    /// <summary>Окно больше не держится: якорем снова становится блок у верха видимой области.</summary>
    internal void ReleaseWindow() => _hold = false;

    /// <summary>Окно уходит на другие строки: оно больше не держится, и его переход следующая раскладка не возмещает.</summary>
    internal void LetWindowGo()
    {
        _hold = false;
        _anchor = null;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // BYDESIGN: обращение к InternalChildren создаёт генератор контейнеров — до него
        // ItemContainerGenerator не работает.
        _ = InternalChildren;

        var owner = ItemsControl.GetItemsOwner(this);

        // BYDESIGN: элемент, заменённый при живом контейнере, WPF привязывает к прежнему контейнеру без
        // события о смене элементов — а окно могло встать внутрь него или уйти; это ловит номер раскладки.
        if (FindViewer() is { } viewer && viewer.WindowVersion != _windowVersion)
        {
            _windowVersion = viewer.WindowVersion;
            _keysChanged = true;
        }

        UpdateKeys(owner);
        UpdateOffsets();

        var constraint = new Size(availableSize.Width, double.PositiveInfinity);

        // BYDESIGN: окно создаётся раньше остальных — его высота нужна якорю, где бы окно ни стояло
        if (_pinned >= 0)
        {
            Realize(_pinned, _pinned, constraint);
            UpdateOffsets();
        }

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

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var generator = ItemContainerGenerator;
        var moved = _arranged.Count != InternalChildren.Count;

        for (var i = 0; i < InternalChildren.Count; i++)
        {
            var child = InternalChildren[i];
            var index = generator.IndexFromGeneratorPosition(new(i, 0));
            var rect = new Rect(0, _tops[index], finalSize.Width, child.DesiredSize.Height);
            child.Arrange(rect);

            if (i < _arranged.Count && ReferenceEquals(_arranged[i].Child, child) && _arranged[i].Rect == rect)
            {
                continue;
            }

            moved = true;
            if (i < _arranged.Count)
            {
                _arranged[i] = (child, rect);
            }
            else
            {
                _arranged.Add((child, rect));
            }
        }

        if (_arranged.Count > InternalChildren.Count)
        {
            _arranged.RemoveRange(InternalChildren.Count, _arranged.Count - InternalChildren.Count);
        }

        var viewer = FindViewer();
        if (moved)
        {
            viewer?.OnBlocksArranged();
        }

        if (_hasGaps)
        {
            InvalidateVisual();
        }

        viewer?.OnViewportMaybeChanged();
        return finalSize;
    }

    /// <summary>Hatches the empty places before and after blocks.</summary>
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (!_hasGaps || TryFindResource("Md.Gap.Brush") is not Brush brush)
        {
            return;
        }

        for (var i = 0; i < _tops.Length; i++)
        {
            if (_before[i] > 0)
            {
                drawingContext.DrawRectangle(brush, null, new(0, _offsets[i], ActualWidth, _before[i]));
            }

            if (_after[i] > 0)
            {
                drawingContext.DrawRectangle(brush, null, new(0, _offsets[i + 1] - _after[i], ActualWidth, _after[i]));
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
    {
        base.OnIsItemsHostChanged(oldIsItemsHost, newIsItemsHost);
        _keysChanged = true;

        if (newIsItemsHost && ItemsControl.GetItemsOwner(this) is MarkdownViewer viewer)
        {
            viewer.BlockPanel = this;
        }
    }

    /// <summary>Moves the nearest scrolling so that the block is visible as a whole or at least from the top.</summary>
    protected override void BringIndexIntoView(int index)
    {
        if (index < 0 || index >= _keys.Length || FindViewport() is not { PhysicalScroller: { } scroller } viewport)
        {
            return;
        }

        var top = GetItemTop(index);
        var bottom = GetItemBottom(index);
        var shift = top < viewport.Top || bottom - top > viewport.Bottom - viewport.Top
            ? top - viewport.Top
            : bottom > viewport.Bottom
                ? bottom - viewport.Bottom
                : 0;

        if (shift != 0)
        {
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset + shift);
        }
    }

    /// <summary>
    /// Removes the controls of removed, replaced and moved items: of a moved one — from its former
    /// place.
    /// </summary>
    protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
        base.OnItemsChanged(sender, args);
        _keysChanged = true;

        var position = args.Action switch
        {
            NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace => args.Position,
            NotifyCollectionChangedAction.Move => args.OldPosition,
            _ => (GeneratorPosition?)null
        };

        if (position is { } at && args.ItemUICount > 0)
        {
            RemoveInternalChildRange(at.Offset > 0 ? at.Index + 1 : at.Index, args.ItemUICount);
        }
    }

    /// <summary>
    /// Насколько с прошлого раза сдвинулся якорь — блок, бывший вверху видимой области, или
    /// удерживаемое окно; если прокрутку с тех пор двигал кто-то другой — ноль, и окно больше не
    /// держится.
    /// </summary>
    private double AnchorShift(Viewport viewport)
    {
        if (viewport.PhysicalScroller is null || _anchor is not { } anchor)
        {
            return 0;
        }

        if (Math.Abs(viewport.Top - anchor.ViewportTop) > 0.5)
        {
            _hold = false;
            return 0;
        }

        var index = anchor.Index < _keys.Length && _keys[anchor.Index] == anchor.Key ? anchor.Index : Array.IndexOf(_keys, anchor.Key);
        return index >= 0 ? _tops[index] - anchor.Top : 0;
    }

    private void RememberAnchor(Viewport viewport)
    {
        var index = AnchorIndex(viewport);
        _anchor = index >= 0 ? (_keys[index], index, _tops[index], viewport.Top) : null;
    }

    /// <summary>Блок-якорь: окно, пока оно держится, иначе блок у верха видимой области; якоря нет — -1.</summary>
    private int AnchorIndex(Viewport viewport)
    {
        if (viewport.PhysicalScroller is null)
        {
            return -1;
        }

        if (_hold && _pinned >= 0)
        {
            return _pinned;
        }

        return viewport.Top > 0 ? IndexAt(viewport.Top) : -1;
    }

    private (int First, int Last) RealizeVisible(Viewport viewport, Size constraint, out double shift)
    {
        shift = 0;

        var anchor = AnchorIndex(viewport);
        var anchorTop = anchor >= 0 ? _tops[anchor] : 0;
        var cache = (viewport.Bottom - viewport.Top) * CacheShare;
        var range = (First: 0, Last: -1);

        // BYDESIGN: созданные блоки остаются, пока не уйдут дальше двойного запаса: иначе блок у края
        // запаса, чья высота отличается от оценки, сдвигает прокрутку, выпадает и возвращается бесконечно.
        var kept = Kept(RangeFor(viewport.Top - 2 * cache, viewport.Bottom + 2 * cache));

        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var next = Union(RangeFor(viewport.Top + shift - cache, viewport.Bottom + shift + cache), kept);
            if (pass > 0 && next.First >= range.First && next.Last <= range.Last)
            {
                break;
            }

            range = next;
            Realize(range.First, range.Last, constraint);
            UpdateOffsets();

            if (anchor >= 0)
            {
                shift = _tops[anchor] - anchorTop;
            }
        }

        return range;
    }

    /// <summary>Кусок номеров, который покрывает оба; пустой кусок не в счёт.</summary>
    private static (int First, int Last) Union((int First, int Last) range, (int First, int Last) other)
    {
        if (other.Last < other.First)
        {
            return range;
        }

        return range.Last < range.First ? other : (Math.Min(range.First, other.First), Math.Max(range.Last, other.Last));
    }

    /// <summary>Созданные блоки внутри окна <paramref name="window"/>.</summary>
    private (int First, int Last) Kept((int First, int Last) window)
    {
        if (RealizedRange is not { } realized)
        {
            return (0, -1);
        }

        var first = Math.Max(realized.First, window.First);
        var last = Math.Min(realized.Last, window.Last);
        return first <= last ? (first, last) : (0, -1);
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
                Remember(index, child.DesiredSize.Height - _inside[index]);
            }
        }
    }

    /// <summary>
    /// Запоминает измеренную высоту блока; смещения пересчитываются с него. Окно в оценку высоты
    /// несозданных блоков не идёт.
    /// </summary>
    private void Remember(int index, double height)
    {
        _heights[_keys[index]] = height;

        var old = _measured[index];
        if (old == height)
        {
            return;
        }

        if (index != _pinned)
        {
            var first = double.IsNaN(old);
            AddMeasured(index, first ? height : height - old, first ? 1 : 0);
        }

        _measured[index] = height;
        _changedFrom = Math.Min(_changedFrom, index);
    }

    /// <summary>Добавляет к суммам высот — общей и вида блока — высоту и число измеренных блоков.</summary>
    private void AddMeasured(int index, double height, int count)
    {
        _measuredSum += height;
        _measuredCount += count;

        var (sum, total) = _kindHeights[_kinds[index]];
        _kindHeights[_kinds[index]] = (sum + height, total + count);
    }

    /// <summary>Номер вида блока — типа его вьюмодели.</summary>
    private int KindOf(object item)
    {
        var type = item.GetType();
        if (!_kindOf.TryGetValue(type, out var kind))
        {
            kind = _kindHeights.Count;
            _kindOf[type] = kind;
            _kindHeights.Add(default);
        }

        return kind;
    }

    private void CleanUp(int first, int last)
    {
        var generator = ItemContainerGenerator;

        for (var i = InternalChildren.Count - 1; i >= 0; i--)
        {
            var position = new GeneratorPosition(i, 0);
            var index = generator.IndexFromGeneratorPosition(position);
            if ((index >= first && index <= last) || index == _pinned)
            {
                continue;
            }

            generator.Remove(position, 1);
            RemoveInternalChildRange(i, 1);
        }
    }

    /// <summary>Ключи, пустые места и известные высоты блоков — после изменения коллекции или пустых мест.</summary>
    private void UpdateKeys(ItemsControl? owner)
    {
        var count = owner?.Items.Count ?? 0;
        if (!_keysChanged && _keys.Length == count)
        {
            return;
        }

        _keysChanged = false;
        if (_keys.Length != count)
        {
            _keys = new HeightKey[count];
            _kinds = new int[count];
            _before = new double[count];
            _after = new double[count];
            _inside = new double[count];
            _measured = new double[count];
        }

        var gaps = GetValue(GapsProperty) as GapLayout ?? GapLayout.Empty;
        var viewer = FindViewer();
        _hasGaps = false;
        _pinned = -1;
        _measuredSum = 0;
        _measuredCount = 0;
        for (var kind = 0; kind < _kindHeights.Count; kind++)
        {
            _kindHeights[kind] = default;
        }

        for (var i = 0; i < count; i++)
        {
            var item = owner!.Items[i];
            _keys[i] = HeightKey.Of(item);
            _kinds[i] = KindOf(item);
            _before[i] = gaps.Before(item);
            _after[i] = gaps.After(item);
            _inside[i] = gaps.Inside(item);
            _hasGaps |= _before[i] > 0 || _after[i] > 0;
            if (item is SourceWindowVm || viewer?.HoldsWindow(item) == true)
            {
                _pinned = i;
            }

            if (_heights.TryGetValue(_keys[i], out var height))
            {
                _measured[i] = height;
                if (i != _pinned)
                {
                    AddMeasured(i, height, 1);
                }
            }
            else
            {
                _measured[i] = double.NaN;
            }
        }

        if (_pinned < 0)
        {
            _hold = false;
        }

        _estimates = [];

        if (_heights.Count > 2 * count + 64)
        {
            var current = _keys.ToHashSet();
            foreach (var key in _heights.Keys.Where(key => !current.Contains(key)).ToList())
            {
                _heights.Remove(key);
            }
        }
    }

    /// <summary>
    /// Смещения блоков: с первого блока, чья высота изменилась, а при новой оценке высоты
    /// неизмеренных — с начала.
    /// </summary>
    private void UpdateOffsets()
    {
        if (_tops.Length != _keys.Length)
        {
            _offsets = new double[_keys.Length + 1];
            _tops = new double[_keys.Length];
            _estimates = [];
        }

        var estimates = Estimates();
        var from = estimates.AsSpan().SequenceEqual(_estimates) ? _changedFrom : 0;
        _estimates = estimates;
        _changedFrom = int.MaxValue;

        for (var i = from; i < _keys.Length; i++)
        {
            var height = (double.IsNaN(_measured[i]) ? estimates[_kinds[i]] : _measured[i]) + _inside[i];
            _tops[i] = _offsets[i] + _before[i];
            _offsets[i + 1] = _tops[i] + height + _after[i];
        }
    }

    /// <summary>Оценки высоты неизмеренных блоков по видам: среднее измеренных того же вида, а без них — всех.</summary>
    private double[] Estimates()
    {
        var overall = _measuredCount > 0 ? _measuredSum / _measuredCount : DefaultHeight;
        var estimates = new double[_kindHeights.Count];
        for (var kind = 0; kind < estimates.Length; kind++)
        {
            var (sum, count) = _kindHeights[kind];
            estimates[kind] = count > 0 ? sum / count : overall;
        }

        return estimates;
    }

    private static void OnGapsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (MdBlockPanel)d;
        panel._keysChanged = true;
        panel.InvalidateMeasure();
        panel.InvalidateVisual();
    }

    /// <summary>Первый блок, чей низ ниже <paramref name="y"/>; такого нет — -1.</summary>
    private int IndexAt(double y)
    {
        var index = FirstOffsetAbove(y, 1);
        return index < _offsets.Length ? index - 1 : -1;
    }

    private (int First, int Last) RangeFor(double top, double bottom)
    {
        var first = IndexAt(Math.Max(top, 0));
        if (first < 0 || bottom <= 0)
        {
            return (0, -1);
        }

        var last = Math.Min(FirstOffsetAtOrAbove(bottom, first + 1), _keys.Length) - 1;
        return (first, last);
    }

    /// <summary>Первый номер смещения не меньше <paramref name="start"/>, где смещение больше <paramref name="y"/>.</summary>
    private int FirstOffsetAbove(double y, int start)
    {
        var low = start;
        var high = _offsets.Length;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (_offsets[middle] > y)
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        return low;
    }

    /// <summary>Первый номер смещения не меньше <paramref name="start"/>, где смещение не меньше <paramref name="y"/>.</summary>
    private int FirstOffsetAtOrAbove(double y, int start)
    {
        var low = start;
        var high = _offsets.Length;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (_offsets[middle] >= y)
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        return low;
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
                .TransformBounds(new(0, 0, scrollViewer.ActualWidth, height));

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
                    ? new(bounds.Top, bounds.Bottom, scrollViewer, scrollInfo)
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