namespace MarkdigWpfViewer.Showcase.Showcases;

using System.Windows;
using System.Windows.Media;

/// <summary>Поиск элементов в визуальном дереве витрины.</summary>
public static class Visuals
{
    /// <summary>Первый потомок типа <typeparamref name="T"/> обходом в глубину; нет такого — <c>null</c>.</summary>
    public static T? FirstDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if ((child as T ?? FirstDescendant<T>(child)) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
