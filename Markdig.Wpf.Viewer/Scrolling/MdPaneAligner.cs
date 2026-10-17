namespace MarkdigWpfViewer.Scrolling;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

/// <summary>
/// Две панели вровень: расставляет пустые места так, чтобы каждая пара карты строк стояла в обеих
/// панелях на одной высоте. Вьюеры стоят в общей прокрутке или в двух своих — тогда их смещения
/// держатся равными.
/// </summary>
public sealed class MdPaneAligner : IDisposable
{
    /// <summary>Расхождение меньше этого пустым местом не закрывается.</summary>
    private const double Tolerance = 0.5;

    private readonly MarkdownViewer _left;
    private readonly MarkdownViewer _right;
    private MdLineMap _map;
    private ScrollViewer? _leftScroller;
    private ScrollViewer? _rightScroller;
    private MarkdownViewer _leading;
    private bool _alignQueued;
    private bool _syncing;
    private bool _disposed;

    public MdPaneAligner(MarkdownViewer left, MarkdownViewer right, MdLineMap map)
    {
        _left = left;
        _right = right;
        _map = map;
        _leading = left;

        left.ViewportChanged += OnViewportChanged;
        right.ViewportChanged += OnViewportChanged;
        Attach(left);
        Attach(right);
        QueueAlign();
    }

    /// <summary>Соответствие строк; новая карта — например, после нового диффа — расставляет места заново.</summary>
    public MdLineMap Map
    {
        get => _map;
        set
        {
            _map = value;
            QueueAlign();
        }
    }

    /// <summary>Расставляет пустые места по нынешней раскладке прямо сейчас.</summary>
    public void Align()
    {
        if (_disposed)
        {
            return;
        }

        var anchors = Anchors();
        var left = NaturalOffsets(_left, anchors.Select(anchor => anchor.Left).ToList());
        var right = NaturalOffsets(_right, anchors.Select(anchor => anchor.Right).ToList());

        var leftGaps = new List<MdGap>();
        var rightGaps = new List<MdGap>();
        var leftShift = 0.0;
        var rightShift = 0.0;

        for (var i = 0; i < anchors.Count; i++)
        {
            if (left[i] is not { } leftTop || right[i] is not { } rightTop)
            {
                continue;
            }

            var difference = rightTop + rightShift - (leftTop + leftShift);
            if (difference > Tolerance)
            {
                leftGaps.Add(new(anchors[i].Left, difference));
                leftShift += difference;
            }
            else if (difference < -Tolerance)
            {
                rightGaps.Add(new(anchors[i].Right, -difference));
                rightShift -= difference;
            }
        }

        SetGaps(_left, leftGaps);
        SetGaps(_right, rightGaps);
    }

    /// <summary>Убирает пустые места и отцепляется от вьюеров.</summary>
    public void Dispose()
    {
        _disposed = true;
        _left.ViewportChanged -= OnViewportChanged;
        _right.ViewportChanged -= OnViewportChanged;
        Detach(_left, ref _leftScroller);
        Detach(_right, ref _rightScroller);
        _left.Gaps = null;
        _right.Gaps = null;
    }

    /// <summary>Целые опорные пары карты по возрастанию; карта один к одному — каждая строка с той же.</summary>
    private List<(int Left, int Right)> Anchors()
    {
        if (_map.Anchors.Count == 0)
        {
            var lines = Math.Min(LineCount(_left), LineCount(_right));
            return Enumerable.Range(0, lines).Select(line => (line, line)).ToList();
        }

        return _map.Anchors
            .Where(anchor => anchor.Left % 1 == 0 && anchor.Right % 1 == 0 && anchor.Left >= 0 && anchor.Right >= 0)
            .Select(anchor => ((int)anchor.Left, (int)anchor.Right))
            .Distinct()
            .OrderBy(anchor => anchor.Item1)
            .ThenBy(anchor => anchor.Item2)
            .ToList();
    }

    private static int LineCount(MarkdownViewer viewer) => (viewer.Markdown ?? string.Empty).Split('\n').Length;

    /// <summary>
    /// Где в содержимом прокрутки вьюера стояли бы пустые места строк, не будь у вьюера мест: верх
    /// того, перед чем место встаёт.
    /// </summary>
    private static double?[] NaturalOffsets(MarkdownViewer viewer, IReadOnlyList<int> lines)
    {
        var origin = ContentTop(viewer);
        return viewer.GetGapOffsets(lines)
            .Select(offset => offset + origin)
            .ToArray();
    }

    /// <summary>Верх вьюера в содержимом его прокрутки; прокрутки нет — ноль.</summary>
    private static double ContentTop(MarkdownViewer viewer)
        => Scrollers.Nearest(viewer) is { } scroller
            ? viewer.TranslatePoint(default, scroller).Y + scroller.VerticalOffset
            : 0;

    private static void SetGaps(MarkdownViewer viewer, List<MdGap> gaps)
    {
        var current = viewer.Gaps ?? [];
        var same = current.Count == gaps.Count
                   && current.Zip(gaps).All(pair => pair.First.Line == pair.Second.Line
                                                     && Math.Abs(pair.First.Height - pair.Second.Height) < Tolerance);
        if (!same)
        {
            viewer.Gaps = gaps;
        }
    }

    private void QueueAlign()
    {
        if (_alignQueued || _disposed)
        {
            return;
        }

        _alignQueued = true;
        _left.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            () =>
            {
                _alignQueued = false;
                Align();
            });
    }

    private void OnViewportChanged(object? sender, EventArgs e) => QueueAlign();

    private void Attach(MarkdownViewer viewer)
    {
        if (!viewer.IsLoaded)
        {
            viewer.Loaded += OnViewerLoaded;
            return;
        }

        if (Scrollers.Nearest(viewer) is not { } scroller)
        {
            return;
        }

        scroller.ScrollChanged += OnScrollChanged;
        scroller.PreviewMouseWheel += OnUserInput;
        scroller.PreviewMouseDown += OnUserInput;
        scroller.PreviewKeyDown += OnUserInput;

        if (ReferenceEquals(viewer, _left))
        {
            _leftScroller = scroller;
        }
        else
        {
            _rightScroller = scroller;
        }

        QueueAlign();
    }

    private void Detach(MarkdownViewer viewer, ref ScrollViewer? scroller)
    {
        viewer.Loaded -= OnViewerLoaded;
        if (scroller is null)
        {
            return;
        }

        scroller.ScrollChanged -= OnScrollChanged;
        scroller.PreviewMouseWheel -= OnUserInput;
        scroller.PreviewMouseDown -= OnUserInput;
        scroller.PreviewKeyDown -= OnUserInput;
        scroller = null;
    }

    private void OnViewerLoaded(object sender, RoutedEventArgs e)
    {
        var viewer = (MarkdownViewer)sender;
        viewer.Loaded -= OnViewerLoaded;
        Attach(viewer);
    }

    private void OnUserInput(object sender, InputEventArgs e)
    {
        _leading = ReferenceEquals(sender, _leftScroller) ? _left : _right;
    }

    /// <summary>У вьюеров свои прокрутки — вторая встаёт на смещение ведущей.</summary>
    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_syncing
            || _leftScroller is null
            || _rightScroller is null
            || ReferenceEquals(_leftScroller, _rightScroller)
            || !ReferenceEquals(e.OriginalSource, sender)
            || e.VerticalChange == 0)
        {
            return;
        }

        var (leader, follower) = ReferenceEquals(_leading, _left) ? (_leftScroller, _rightScroller) : (_rightScroller, _leftScroller);
        _syncing = true;
        try
        {
            follower.ScrollToVerticalOffset(leader.VerticalOffset);
        }
        finally
        {
            _syncing = false;
        }
    }
}
