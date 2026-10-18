namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Selection;

/// <summary>
/// Как пометки текста ложатся на неделимый кусок: заливка — фоном, черта и рамка — краем; вертикальной
/// черты у неделимого куска нет.
/// </summary>
internal static class MarkLook
{
    /// <summary>Фон, кисть и толщина края от пометок в порядке рисования: из одинаковых видов — последняя.</summary>
    public static (Brush? Fill, Brush? Line, Thickness Thickness) Of(
        IEnumerable<(MdMarkKind Kind, Brush Brush)> marks,
        FrameworkElement element)
    {
        Brush? fill = null;
        Brush? line = null;
        var thickness = new Thickness();

        foreach (var (kind, brush) in marks)
        {
            switch (kind)
            {
                case MdMarkKind.Underline:
                    line = brush;
                    thickness = new(0, 0, 0, UnderlineThickness(element));
                    break;

                case MdMarkKind.Frame:
                    line = brush;
                    thickness = new(FrameThickness(element));
                    break;

                case MdMarkKind.Caret:
                    break;

                default:
                    fill = brush;
                    break;
            }
        }

        return (fill, line, thickness);
    }

    /// <summary>Ставит пометки на накладку неделимого куска.</summary>
    public static void Apply(Border overlay, FrameworkElement element, IEnumerable<(MdMarkKind Kind, Brush Brush)> marks)
    {
        var (fill, line, thickness) = Of(marks, element);
        overlay.Background = fill;
        overlay.BorderBrush = line;
        overlay.BorderThickness = thickness;
    }

    public static double UnderlineThickness(FrameworkElement element)
        => element.TryFindResource("Md.Mark.UnderlineThickness") is double value ? value : 2;

    public static double FrameThickness(FrameworkElement element)
        => element.TryFindResource("Md.Mark.FrameThickness") is double value ? value : 1;

    public static double CaretThickness(FrameworkElement element)
        => element.TryFindResource("Md.Mark.CaretThickness") is double value ? value : 2;
}
