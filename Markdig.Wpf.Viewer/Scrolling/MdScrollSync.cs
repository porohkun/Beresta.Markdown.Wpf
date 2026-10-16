namespace MarkdigWpfViewer.Scrolling;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

/// <summary>
/// Синхронная прокрутка двух вьюеров по соответствию строк. Ведущий — тот, с которым работает
/// пользователь; второй догоняет его так, чтобы в точке привязки стояли соответствующие строки.
/// </summary>
public sealed class MdScrollSync : IDisposable
{
    private readonly MarkdownViewer _left;
    private readonly MarkdownViewer _right;
    private readonly MdLineMap _map;
    private readonly double _anchorFraction;
    private ScrollViewer? _leftScroller;
    private ScrollViewer? _rightScroller;
    private MarkdownViewer _leading;

    /// <param name="anchorFraction">Точка привязки — доля высоты видимой области сверху.</param>
    public MdScrollSync(MarkdownViewer left, MarkdownViewer right, MdLineMap map, double anchorFraction = 0.38)
    {
        _left = left;
        _right = right;
        _map = map;
        _anchorFraction = anchorFraction;
        _leading = left;

        Attach(left);
        Attach(right);
    }

    public void Dispose()
    {
        Detach(_left, ref _leftScroller);
        Detach(_right, ref _rightScroller);
    }

    /// <summary>Подтягивает второй вьюер к ведущему прямо сейчас.</summary>
    public void Sync()
    {
        var follower = ReferenceEquals(_leading, _left) ? _right : _left;
        if (_leading.GetVisibleSourceLine(_anchorFraction) is not { } line)
        {
            return;
        }

        var target = ReferenceEquals(_leading, _left) ? _map.ToRight(line) : _map.ToLeft(line);
        follower.ScrollToSourceLine(target, _anchorFraction);
    }

    private void Attach(MarkdownViewer viewer)
    {
        if (!viewer.IsLoaded)
        {
            viewer.Loaded += OnViewerLoaded;
            return;
        }

        var scroller = Scrollers.Nearest(viewer);
        if (scroller is null)
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

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var fromLeading = ReferenceEquals(sender, ReferenceEquals(_leading, _left) ? _leftScroller : _rightScroller);
        if (fromLeading && ReferenceEquals(e.OriginalSource, sender) && (e.VerticalChange != 0 || e.ViewportHeightChange != 0))
        {
            Sync();
        }
    }
}