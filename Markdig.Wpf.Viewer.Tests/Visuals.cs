namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Media;
using Presenters;

/// <summary>Обход визуального дерева и точки знаков во вьюере.</summary>
internal static class Visuals
{
    public static IEnumerable<T> Descendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T found)
            {
                yield return found;
            }

            foreach (var deeper in Descendants<T>(child))
            {
                yield return deeper;
            }
        }
    }

    /// <summary>Точка у левого края знака карты в координатах вьюера; знак должен быть в созданном блоке.</summary>
    public static Point PointOf(MarkdownViewer viewer, int offset)
    {
        foreach (var text in Descendants<MdTextBlock>(viewer))
        {
            if (text.TryGetMapRange(out var range) && offset >= range.Start && offset < range.End)
            {
                var rect = text.GetRangeRects(offset - range.Start, offset - range.Start + 1).First();
                return text.TranslatePoint(new(rect.Left + 0.5, rect.Top + rect.Height / 2), viewer);
            }
        }

        throw new InvalidOperationException($"Знак {offset} не найден среди созданных блоков.");
    }
}