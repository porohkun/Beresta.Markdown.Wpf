namespace MarkdigWpfViewer.Selection;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Presenters;
using Scrolling;

/// <summary>Выделение во вьюере: якорь и активный конец в текстовой карте, мышь, автопрокрутка.</summary>
internal sealed class SelectionController
{
    private const double MaxScrollStep = 60;

    private readonly MarkdownViewer _viewer;
    private readonly DispatcherTimer _autoScroll;
    private int _anchor;
    private int _active;
    private bool _pressed;
    private bool _dragging;
    private Point _pressPoint;
    private ScrollViewer? _scroller;
    private Point _pointInScroller;

    public SelectionController(MarkdownViewer viewer)
    {
        _viewer = viewer;
        _autoScroll = new(TimeSpan.FromMilliseconds(30), DispatcherPriority.Input, (_, _) => AutoScrollStep(), viewer.Dispatcher);
        _autoScroll.Stop();
    }

    public event Action? Changed;

    private enum WordKind
    {
        Word,
        Space,
        Other
    }

    public MdTextRange Range => MdTextRange.Between(_anchor, _active);

    /// <summary>Якорь и активный конец в текстовой карте.</summary>
    public (int Anchor, int Active) Ends => (_anchor, _active);

    public static bool IsInteractive(DependencyObject? source, DependencyObject viewer)
    {
        for (var node = source; node is not null && !ReferenceEquals(node, viewer); node = ParentOf(node))
        {
            if (node is ButtonBase or TextBoxBase or PasswordBox or RangeBase or Thumb or Selector
                || MdSelection.GetIsInteractive(node))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Созданные элементы со своим куском карты: тексты блоков и неделимые блоки.</summary>
    public static IEnumerable<FrameworkElement> Targets(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            switch (child)
            {
                case MdTextBlock text when text.TryGetMapRange(out _):
                    yield return text;
                    continue;
                case MdBlockItem item when item.TryGetAtomicRange(out _):
                    yield return item;
                    continue;
            }

            foreach (var deeper in Targets(child))
            {
                yield return deeper;
            }
        }
    }

    /// <summary>Выделяет кусок; при выключенном выделении — только ставит пустое на место.</summary>
    public void Select(int start, int length)
    {
        var size = _viewer.TextMap.Length;
        _anchor = Math.Clamp(start, 0, size);
        _active = _viewer.IsSelectionEnabled ? Math.Clamp(start + length, 0, size) : _anchor;
        Changed?.Invoke();
    }

    public void Reset()
    {
        _pressed = false;
        _dragging = false;
        _autoScroll.Stop();
        Select(0, 0);
    }

    /// <summary>Ставит концы, перенесённые в новую текстовую карту.</summary>
    public void Restore(int anchor, int active)
    {
        var size = _viewer.TextMap.Length;
        _anchor = Math.Clamp(anchor, 0, size);
        _active = Math.Clamp(active, 0, size);
        Changed?.Invoke();
    }

    /// <summary>
    /// Позиция в карте для точки во вьюере; с <paramref name="snapToText"/> — у ближайшего текста,
    /// без него вне текста — <c>null</c>.
    /// </summary>
    public int? FindOffset(Point point, bool snapToText)
    {
        if (_viewer.TextMap.Length == 0)
        {
            return snapToText ? 0 : null;
        }

        if (TargetAt(point, snapToText) is not { } found)
        {
            return null;
        }

        var local = _viewer.TranslatePoint(point, found.Target);
        return found.Target is MdTextBlock text
            ? found.Range.Start + text.HitTestLocal(local)
            : AtomicOffset(found.Target, found.Range, local);
    }

    /// <summary>Двойной щелчок: выделяет слово под точкой, а среди пробелов — пробелы, иначе один знак.</summary>
    public bool SelectWordAt(Point point)
    {
        if (!_viewer.IsSelectionEnabled || FindOffset(point, snapToText: true) is not { } offset)
        {
            return false;
        }

        var (start, end) = WordAround(_viewer.TextMap.Text, offset);
        SelectBetween(start, end);
        return true;
    }

    /// <summary>Тройной щелчок: выделяет весь текст блока под точкой — абзац, заголовок, ячейку, код.</summary>
    public bool SelectBlockAt(Point point)
    {
        if (!_viewer.IsSelectionEnabled || _viewer.TextMap.Length == 0 || TargetAt(point, snapToText: true) is not { } found)
        {
            return false;
        }

        SelectBetween(found.Range.Start, found.Range.End);
        return true;
    }

    /// <summary>Нажатие: ставит якорь или, с <paramref name="extend"/>, двигает активный конец.</summary>
    public bool Press(Point point, bool extend)
    {
        if (!_viewer.IsSelectionEnabled || FindOffset(point, snapToText: true) is not { } offset)
        {
            return false;
        }

        if (!extend)
        {
            _anchor = offset;
        }

        _active = offset;
        _pressed = true;
        _dragging = false;
        _pressPoint = point;
        Changed?.Invoke();
        return true;
    }

    /// <summary>Перетаскивание; <c>true</c> — выделение тянется мышью.</summary>
    public bool Drag(Point point)
    {
        if (!_pressed)
        {
            return false;
        }

        if (!_dragging)
        {
            var moved = Math.Abs(point.X - _pressPoint.X) >= SystemParameters.MinimumHorizontalDragDistance
                        || Math.Abs(point.Y - _pressPoint.Y) >= SystemParameters.MinimumVerticalDragDistance;
            if (!moved)
            {
                return false;
            }

            _dragging = true;
        }

        MoveActive(point);
        UpdateAutoScroll(point);
        return true;
    }

    /// <summary>Отпускание; <c>true</c> — это было перетаскивание, а не щелчок.</summary>
    public bool Release()
    {
        var wasDragging = _dragging;
        _pressed = false;
        _dragging = false;
        _autoScroll.Stop();
        return wasDragging;
    }

    /// <summary>Шаг автопрокрутки, пока мышь за краем видимой области.</summary>
    internal void AutoScrollStep()
    {
        if (!_dragging || _scroller is null)
        {
            _autoScroll.Stop();
            return;
        }

        var height = _scroller.ActualHeight;
        var distance = _pointInScroller.Y < 0 ? _pointInScroller.Y : _pointInScroller.Y - height;
        if (_pointInScroller.Y >= 0 && _pointInScroller.Y <= height)
        {
            _autoScroll.Stop();
            return;
        }

        if (_scroller.CanContentScroll)
        {
            if (distance < 0)
            {
                _scroller.LineUp();
            }
            else
            {
                _scroller.LineDown();
            }
        }
        else
        {
            var step = Math.Clamp(distance / 2, -MaxScrollStep, MaxScrollStep);
            _scroller.ScrollToVerticalOffset(_scroller.VerticalOffset + step);
        }

        _scroller.UpdateLayout();
        MoveActive(_scroller.TranslatePoint(_pointInScroller, _viewer));
    }

    private static DependencyObject? ParentOf(DependencyObject node)
    {
        return node is Visual or Visual3D
            ? VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node)
            : LogicalTreeHelper.GetParent(node);
    }

    private static int AtomicOffset(FrameworkElement element, MdTextRange range, Point pointInElement)
        => pointInElement.Y > element.ActualHeight / 2 ? range.End : range.Start;

    private static WordKind KindOf(char symbol)
        => char.IsLetterOrDigit(symbol) || symbol == '_' ? WordKind.Word
            : symbol != '\n' && char.IsWhiteSpace(symbol) ? WordKind.Space
            : WordKind.Other;

    /// <summary>Слово вокруг позиции: знак справа от неё, а если там не слово — знак слева.</summary>
    private static (int Start, int End) WordAround(string text, int offset)
    {
        var at = offset < text.Length && KindOf(text[offset]) == WordKind.Word ? offset
            : offset > 0 && KindOf(text[offset - 1]) == WordKind.Word ? offset - 1
            : offset;
        if (at >= text.Length || text[at] == '\n')
        {
            return (offset, offset);
        }

        var kind = KindOf(text[at]);
        if (kind == WordKind.Other)
        {
            return (at, at + 1);
        }

        var start = at;
        while (start > 0 && KindOf(text[start - 1]) == kind)
        {
            start--;
        }

        var end = at + 1;
        while (end < text.Length && KindOf(text[end]) == kind)
        {
            end++;
        }

        return (start, end);
    }

    private void SelectBetween(int start, int end)
    {
        _pressed = false;
        _dragging = false;
        _anchor = start;
        _active = end;
        Changed?.Invoke();
    }

    /// <summary>Элемент со своим куском карты под точкой; с привязкой к тексту — ближайший к ней.</summary>
    private (FrameworkElement Target, MdTextRange Range)? TargetAt(Point point, bool snapToText)
    {
        DependencyObject? hit = null;
        VisualTreeHelper.HitTest(
            _viewer,
            null,
            result =>
            {
                hit = result.VisualHit;
                return HitTestResultBehavior.Stop;
            },
            new PointHitTestParameters(point));

        for (var node = hit; node is not null && !ReferenceEquals(node, _viewer); node = ParentOf(node))
        {
            if (node is MdTextBlock text && text.TryGetMapRange(out var textRange))
            {
                return (text, textRange);
            }

            if (node is MdBlockItem item && item.TryGetAtomicRange(out var atomicRange))
            {
                return (item, atomicRange);
            }
        }

        return snapToText ? Nearest(point) : null;
    }

    private void MoveActive(Point point)
    {
        if (FindOffset(point, snapToText: true) is { } offset && offset != _active)
        {
            _active = offset;
            Changed?.Invoke();
        }
    }

    private void UpdateAutoScroll(Point point)
    {
        _scroller ??= Scrollers.Nearest(_viewer);
        if (_scroller is null)
        {
            return;
        }

        _pointInScroller = _viewer.TranslatePoint(point, _scroller);
        if (_pointInScroller.Y < 0 || _pointInScroller.Y > _scroller.ActualHeight)
        {
            _autoScroll.Start();
        }
        else
        {
            _autoScroll.Stop();
        }
    }

    private (FrameworkElement Target, MdTextRange Range)? Nearest(Point point)
    {
        FrameworkElement? best = null;
        var bestDistance = double.MaxValue;

        foreach (var target in Targets(_viewer))
        {
            var bounds = target.TransformToAncestor(_viewer).TransformBounds(new(target.RenderSize));
            var dy = point.Y < bounds.Top ? bounds.Top - point.Y : point.Y > bounds.Bottom ? point.Y - bounds.Bottom : 0;
            var dx = point.X < bounds.Left ? bounds.Left - point.X : point.X > bounds.Right ? point.X - bounds.Right : 0;
            var distance = dy * 1000 + dx;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = target;
            }
        }

        return best switch
        {
            MdTextBlock text when text.TryGetMapRange(out var range) => (text, range),
            MdBlockItem item when item.TryGetAtomicRange(out var range) => (item, range),
            _ => null
        };
    }
}