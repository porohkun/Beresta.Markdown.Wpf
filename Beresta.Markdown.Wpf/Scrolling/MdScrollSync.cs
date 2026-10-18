namespace Beresta.Markdown.Wpf.Scrolling;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

/// <summary>
/// Scrolling of two sides together — viewers, a viewer and an editor — by a line map. The leading
/// side is the one the user works with; the other follows so that corresponding lines stand at the
/// anchor point.
/// </summary>
public sealed class MdScrollSync : IDisposable
{
    private readonly IMdScrollSide _left;
    private readonly IMdScrollSide _right;
    private MdLineMap _map;
    private readonly double _anchorFraction;
    private ScrollViewer? _leftScroller;
    private ScrollViewer? _rightScroller;
    private IMdScrollSide _leading;

    /// <summary>Scrolling of two sides together.</summary>
    /// <param name="left">The left side: the left source of the line map.</param>
    /// <param name="right">The right side: the right source of the line map.</param>
    /// <param name="map">The line map.</param>
    /// <param name="anchorFraction">The anchor point: a fraction of the height of the visible area
    /// from the top.</param>
    public MdScrollSync(IMdScrollSide left, IMdScrollSide right, MdLineMap map, double anchorFraction = 0.38)
    {
        _left = left;
        _right = right;
        _map = map;
        _anchorFraction = anchorFraction;
        _leading = left;

        Attach(left);
        Attach(right);
    }

    /// <summary>Scrolling of two viewers together.</summary>
    /// <param name="left">The left viewer: the left source of the line map.</param>
    /// <param name="right">The right viewer: the right source of the line map.</param>
    /// <param name="map">The line map.</param>
    /// <param name="anchorFraction">The anchor point: a fraction of the height of the visible area
    /// from the top.</param>
    public MdScrollSync(MarkdownViewer left, MarkdownViewer right, MdLineMap map, double anchorFraction = 0.38)
        : this((IMdScrollSide)left, right, map, anchorFraction)
    {
    }

    /// <summary>The line map; a new one — for example, after a new diff — moves the other side at once.</summary>
    public MdLineMap Map
    {
        get => _map;
        set
        {
            _map = value;
            Sync();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Detach(_left, ref _leftScroller);
        Detach(_right, ref _rightScroller);
    }

    /// <summary>Moves the other side to the leading one right now.</summary>
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

    /// <summary>Подписывается на прокрутку стороны; сторона-элемент — когда загрузится.</summary>
    private void Attach(IMdScrollSide side)
    {
        if (side is FrameworkElement { IsLoaded: false } element)
        {
            element.Loaded += OnSideLoaded;
            return;
        }

        if (side.Scroller is not { } scroller)
        {
            return;
        }

        scroller.ScrollChanged += OnScrollChanged;
        scroller.PreviewMouseWheel += OnUserInput;
        scroller.PreviewMouseDown += OnUserInput;
        scroller.PreviewKeyDown += OnUserInput;

        if (ReferenceEquals(side, _left))
        {
            _leftScroller = scroller;
        }
        else
        {
            _rightScroller = scroller;
        }
    }

    private void Detach(IMdScrollSide side, ref ScrollViewer? scroller)
    {
        if (side is FrameworkElement element)
        {
            element.Loaded -= OnSideLoaded;
        }

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

    private void OnSideLoaded(object sender, RoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        element.Loaded -= OnSideLoaded;
        Attach((IMdScrollSide)element);
    }

    private void OnUserInput(object sender, InputEventArgs e)
    {
        _leading = ReferenceEquals(sender, _leftScroller) ? _left : _right;
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var fromLeading = ReferenceEquals(sender, ReferenceEquals(_leading, _left) ? _leftScroller : _rightScroller);
        if (fromLeading
            && ReferenceEquals(e.OriginalSource, sender)
            && (e.VerticalChange != 0 || e.ViewportHeightChange != 0 || e.ExtentHeightChange != 0))
        {
            Sync();
        }
    }
}
