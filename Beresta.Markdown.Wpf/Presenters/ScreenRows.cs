namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ViewModels;

/// <summary>
/// Строки исходника созданных блоков, у которых есть своё место на экране: верх и высота экранной
/// строки в координатах элемента. Текст и код знают свои строки сами; блок без своих строк отдаёт
/// первую строку у верха рамки. Из мест, налезающих друг на друга, остаётся одно.
/// </summary>
internal static class ScreenRows
{
    /// <param name="panel">Панель, чьи созданные блоки обходятся.</param>
    /// <param name="relativeTo">Элемент, в координатах которого места строк.</param>
    /// <param name="frameHeight">Высота места строки у верха рамки блока без своих строк.</param>
    public static IReadOnlyList<(int Line, double Top, double Height)> Collect(
        MdBlockPanel panel,
        UIElement relativeTo,
        Func<int, double> frameHeight)
    {
        var places = new SortedDictionary<int, Place>();
        Collect(panel, null, places, relativeTo, frameHeight);
        return WithoutOverlaps(places);
    }

    /// <summary>Из мест, налезающих друг на друга, оставляет одно: своя строка охотнее, чем у рамки.</summary>
    private static List<(int Line, double Top, double Height)> WithoutOverlaps(SortedDictionary<int, Place> places)
    {
        var placed = new List<(int Line, Place Place)>();

        foreach (var (line, place) in places.OrderBy(pair => pair.Value.Top))
        {
            if (placed.Count > 0 && placed[^1].Place is var last && last.Top + last.Height > place.Top + 0.5)
            {
                if (place.Exact && !last.Exact)
                {
                    placed[^1] = (line, place);
                }

                continue;
            }

            placed.Add((line, place));
        }

        return placed.OrderBy(item => item.Line).Select(item => (item.Line, item.Place.Top, item.Place.Height)).ToList();
    }

    /// <summary>Своя строка охотнее рамки блока, из равных — верхняя.</summary>
    private static void Remember(SortedDictionary<int, Place> places, int line, Place place)
    {
        if (!places.TryGetValue(line, out var known)
            || (place.Exact != known.Exact ? place.Exact : place.Top < known.Top))
        {
            places[line] = place;
        }
    }

    /// <summary>Обходит созданные блоки; внутрь текста и кода не заходит — свои строки они знают сами.</summary>
    private static void Collect(
        DependencyObject node,
        NodeVm? owner,
        SortedDictionary<int, Place> places,
        UIElement relativeTo,
        Func<int, double> frameHeight)
    {
        switch (node)
        {
            case MdTextBlock { ActualHeight: > 0 } text:
                RememberRows(places, text, text.SourceRows, relativeTo);
                return;

            case CodeBlockPresenter { ActualHeight: > 0 } code:
                RememberRows(places, code, code.SourceRows(), relativeTo);
                return;
        }

        var content = node switch
        {
            ContentPresenter presenter => presenter.Content,
            ContentControl control => control.Content,
            _ => null
        };

        if (node is FrameworkElement { ActualHeight: > 0 } element
            && content is NodeVm { Source.IsEmpty: false } model and not InlineVm
            && !ReferenceEquals(model, owner))
        {
            var top = element.TranslatePoint(default, relativeTo).Y;
            if (model is BlankLineVm)
            {
                Remember(places, model.Source.StartLine, new(top, element.ActualHeight, Exact: true));
                return;
            }

            var height = Math.Min(frameHeight(model.Source.StartLine), element.ActualHeight);
            Remember(places, model.Source.StartLine, new(top, height, Exact: false));
            owner = model;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            Collect(VisualTreeHelper.GetChild(node, i), owner, places, relativeTo, frameHeight);
        }
    }

    private static void RememberRows(
        SortedDictionary<int, Place> places,
        UIElement element,
        IEnumerable<(int Line, Rect Row)> rows,
        UIElement relativeTo)
    {
        var top = element.TranslatePoint(default, relativeTo).Y;
        foreach (var (line, row) in rows)
        {
            Remember(places, line, new(top + row.Top, row.Height, Exact: true));
        }
    }

    /// <summary>Где стоит место строки: полоса по вертикали и своя ли это строка или только рамка блока.</summary>
    private readonly record struct Place(double Top, double Height, bool Exact);
}
