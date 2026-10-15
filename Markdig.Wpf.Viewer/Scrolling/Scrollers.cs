namespace MarkdigWpfViewer.Scrolling;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

/// <summary>Поиск прокрутки, в которой стоит элемент.</summary>
internal static class Scrollers
{
    /// <summary>Ближайшая прокрутка-предок, которая крутит по вертикали.</summary>
    public static ScrollViewer? Nearest(DependencyObject element)
    {
        for (var node = VisualTreeHelper.GetParent(element); node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is ScrollViewer { VerticalScrollBarVisibility: not ScrollBarVisibility.Disabled } scroller)
            {
                return scroller;
            }
        }

        return null;
    }
}
