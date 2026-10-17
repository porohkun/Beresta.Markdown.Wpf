namespace MarkdigWpfViewer.Scrolling;

using System.Windows.Controls;

/// <summary>Сторона синхронной прокрутки: строки исходника в своей прокрутке — вьюер, редактор.</summary>
public interface IMdScrollSide
{
    /// <summary>Своя прокрутка стороны; пока её нет — <c>null</c>.</summary>
    ScrollViewer? Scroller { get; }

    /// <summary>
    /// Строка исходника, которая видна в доле <paramref name="viewportFraction"/> высоты своей прокрутки:
    /// 0 — верхний край, 1 — нижний; дробная часть — доля строки.
    /// </summary>
    double? GetVisibleSourceLine(double viewportFraction = 0);

    /// <summary>Прокручивает свою прокрутку так, чтобы строка исходника встала в долю <paramref name="viewportFraction"/> её высоты.</summary>
    bool ScrollToSourceLine(double line, double viewportFraction = 0);
}
