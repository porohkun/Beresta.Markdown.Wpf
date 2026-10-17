namespace MarkdigWpfViewer.Scrolling;

using System.Windows;
using System.Windows.Media;
using Presenters;
using Selection;

/// <summary>
/// Геометрия документа в координатах выбранного элемента: куски текста, строки исходника, точки.
/// Считается при каждом вызове по нынешней раскладке.
/// </summary>
public sealed class MdCoordinates
{
    internal MdCoordinates(MarkdownViewer viewer, UIElement target)
    {
        Viewer = viewer;
        Target = target;
    }

    public MarkdownViewer Viewer { get; }

    /// <summary>Элемент, в координатах которого отвечают методы.</summary>
    public UIElement Target { get; }

    /// <summary>Прямоугольники куска текстовой карты; есть только у созданных блоков.</summary>
    public IReadOnlyList<Rect> RangeRects(MdTextRange range)
        => ToTarget() is { } transform
            ? Viewer.GetRangeRects(range).Select(transform.TransformBounds).ToList()
            : [];

    /// <summary>Рамка куска текстовой карты; кусок не на созданных блоках — <see cref="Rect.Empty"/>.</summary>
    public Rect RangeBounds(MdTextRange range) => RangeRects(range).Aggregate(Rect.Empty, Rect.Union);

    /// <summary>Верх строки исходника; дробная часть — доля строки.</summary>
    public double? LineTop(double line)
        => Viewer.GetSourceLineOffset(line) is { } offset && ToTarget() is { } transform
            ? transform.Transform(new(0, offset)).Y
            : null;

    /// <summary>
    /// Полоса строк исходника с <paramref name="first"/> по <paramref name="last"/> включительно во
    /// всю ширину вьюера: от верха первой до верха следующей за последней.
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

    /// <summary>Строка исходника в точке; дробная часть — доля строки.</summary>
    public double? LineAt(Point point)
        => ToViewer(point) is { } local ? Viewer.GetSourceLineAt(local.Y) : null;

    /// <summary>
    /// Позиция в текстовой карте для точки; с <paramref name="snapToText"/> — у ближайшего текста, без
    /// него вне текста — <c>null</c>.
    /// </summary>
    public int? PositionAt(Point point, bool snapToText)
        => ToViewer(point) is { } local ? Viewer.GetPositionFromPoint(local, snapToText) : null;

    /// <summary>
    /// Строки исходника со своим местом на экране в видимой области прокрутки: верх и высота экранной
    /// строки — там же, где поля ставят номера строк.
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

    /// <summary>Строки исходника у верхнего и нижнего краёв видимой области прокрутки, в которой стоит вьюер.</summary>
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
