namespace Beresta.Markdown.Wpf.Scrolling;

using System.Windows;
using System.Windows.Media;
using Presenters;
using Selection;

/// <summary>
/// The geometry of the document in the coordinates of a chosen element: pieces of text, source
/// lines, points. Computed on every call from the current layout.
/// </summary>
public sealed class MdCoordinates
{
    internal MdCoordinates(MarkdownViewer viewer, UIElement target)
    {
        Viewer = viewer;
        Target = target;
    }

    /// <summary>The viewer whose document is measured.</summary>
    public MarkdownViewer Viewer { get; }

    /// <summary>The element in whose coordinates the methods answer.</summary>
    public UIElement Target { get; }

    /// <summary>Rectangles of a piece of the text map; only created blocks have them.</summary>
    public IReadOnlyList<Rect> RangeRects(MdTextRange range)
        => ToTarget() is { } transform
            ? Viewer.GetRangeRects(range).Select(transform.TransformBounds).ToList()
            : [];

    /// <summary>
    /// The bounds of a piece of the text map; a piece not on created blocks —
    /// <see cref="Rect.Empty"/>.
    /// </summary>
    public Rect RangeBounds(MdTextRange range) => RangeRects(range).Aggregate(Rect.Empty, Rect.Union);

    /// <summary>The top of a source line; the fraction is a part of the line.</summary>
    public double? LineTop(double line)
        => Viewer.GetSourceLineOffset(line) is { } offset && ToTarget() is { } transform
            ? transform.Transform(new(0, offset)).Y
            : null;

    /// <summary>
    /// The band of source lines from <paramref name="first"/> to <paramref name="last"/> inclusive
    /// across the width of the viewer: from the top of the first to the top of the one after the
    /// last.
    /// </summary>
    public Rect? LinesRect(int first, int last)
    {
        if (ToTarget() is not { } transform
            || Viewer.GetSourceLineOffset(first) is not { } top
            || Viewer.GetSourceLineOffset(last + 1) is not { } bottom)
        {
            return null;
        }

        return transform.TransformBounds(new(0, top, Viewer.ActualWidth, Math.Max(0, bottom - top)));
    }

    /// <summary>The source line at a point; the fraction is a part of the line.</summary>
    public double? LineAt(Point point)
        => ToViewer(point) is { } local ? Viewer.GetSourceLineAt(local.Y) : null;

    /// <summary>
    /// The position in the text map for a point; with <paramref name="snapToText"/> — at the
    /// nearest text, without it outside text — <c>null</c>.
    /// </summary>
    public int? PositionAt(Point point, bool snapToText)
        => ToViewer(point) is { } local ? Viewer.GetPositionFromPoint(local, snapToText) : null;

    /// <summary>
    /// Source lines with their own place on screen in the visible area of the scrolling: the top
    /// and height of the screen row — where the margin puts line numbers.
    /// </summary>
    public IReadOnlyList<(int Line, double Top, double Height)> VisibleRows()
    {
        if (Viewer.BlockPanel is not { } panel || ToTarget() is null)
        {
            return [];
        }

        var lineHeight = Viewer.FontSize * Viewer.FontFamily.LineSpacing;
        var rows = ScreenRows.Collect(panel, Target, _ => lineHeight);
        if (Scrollers.Nearest(Viewer) is not { } scroller)
        {
            return rows;
        }

        var viewport = scroller.TransformToVisual(Target).TransformBounds(new(0, 0, scroller.ViewportWidth, scroller.ViewportHeight));
        return rows
            .Where(row => row.Top + row.Height > viewport.Top && row.Top < viewport.Bottom)
            .ToList();
    }

    /// <summary>
    /// The source lines at the top and bottom edges of the visible area of the scrolling that holds
    /// the viewer.
    /// </summary>
    public (int First, int Last)? VisibleLines()
        => Viewer.GetVisibleSourceLine(0) is { } first && Viewer.GetVisibleSourceLine(1) is { } last
            ? ((int)Math.Floor(first), Math.Max((int)Math.Floor(first), (int)Math.Floor(last)))
            : null;

    private GeneralTransform? ToTarget()
    {
        try
        {
            return Viewer.TransformToVisual(Target);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private Point? ToViewer(Point point)
    {
        try
        {
            return Target.TransformToVisual(Viewer).Transform(point);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
