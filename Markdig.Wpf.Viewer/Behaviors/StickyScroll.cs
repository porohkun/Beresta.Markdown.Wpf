namespace MarkdigWpfViewer.Behaviors;

using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Прилипание <see cref="ScrollViewer"/> к краю, куда приходят новые данные: пока прилипла, прокрутка
/// держится у края, как бы ни менялось содержимое. Отлипает, если увести её от края, и прилипает снова у
/// края; без отлипания держится всегда.
/// </summary>
public static class StickyScroll
{
    /// <summary>Расхождение с краем меньше этого считается краем.</summary>
    private const double Tolerance = 1;

    public static readonly DependencyProperty EdgeProperty =
        DependencyProperty.RegisterAttached(
            "Edge",
            typeof(StickyEdge),
            typeof(StickyScroll),
            new(StickyEdge.None, OnEdgeChanged));

    public static readonly DependencyProperty CanUnstickProperty =
        DependencyProperty.RegisterAttached(
            "CanUnstick",
            typeof(bool),
            typeof(StickyScroll),
            new(true, OnCanUnstickChanged));

    private static readonly DependencyPropertyKey IsStuckPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly(
            "IsStuck",
            typeof(bool),
            typeof(StickyScroll),
            new(false));

    /// <summary>Прокрутка сейчас прилипла к своему краю.</summary>
    public static readonly DependencyProperty IsStuckProperty = IsStuckPropertyKey.DependencyProperty;

    public static StickyEdge GetEdge(DependencyObject element)
        => (StickyEdge)element.GetValue(EdgeProperty);

    public static void SetEdge(DependencyObject element, StickyEdge value)
        => element.SetValue(EdgeProperty, value);

    public static bool GetCanUnstick(DependencyObject element)
        => (bool)element.GetValue(CanUnstickProperty);

    public static void SetCanUnstick(DependencyObject element, bool value)
        => element.SetValue(CanUnstickProperty, value);

    public static bool GetIsStuck(DependencyObject element)
        => (bool)element.GetValue(IsStuckProperty);

    private static void OnEdgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scroller)
        {
            return;
        }

        scroller.ScrollChanged -= OnScrollChanged;
        var stuck = (StickyEdge)e.NewValue != StickyEdge.None;
        scroller.SetValue(IsStuckPropertyKey, stuck);
        if (stuck)
        {
            scroller.ScrollChanged += OnScrollChanged;
            Stick(scroller);
        }
    }

    private static void OnCanUnstickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scroller && e.NewValue is false && GetEdge(scroller) != StickyEdge.None)
        {
            scroller.SetValue(IsStuckPropertyKey, true);
            Stick(scroller);
        }
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var scroller = (ScrollViewer)sender;
        if (!ReferenceEquals(e.OriginalSource, scroller))
        {
            return;
        }

        if (e.ExtentHeightChange != 0 || e.ViewportHeightChange != 0)
        {
            if (GetIsStuck(scroller))
            {
                Stick(scroller);
            }

            return;
        }

        if (e.VerticalChange != 0 && GetCanUnstick(scroller))
        {
            scroller.SetValue(IsStuckPropertyKey, AtEdge(scroller));
        }
    }

    private static bool AtEdge(ScrollViewer scroller)
        => GetEdge(scroller) == StickyEdge.Top
            ? scroller.VerticalOffset <= Tolerance
            : scroller.VerticalOffset >= scroller.ScrollableHeight - Tolerance;

    private static void Stick(ScrollViewer scroller)
    {
        if (GetEdge(scroller) == StickyEdge.Top)
        {
            scroller.ScrollToTop();
        }
        else
        {
            scroller.ScrollToBottom();
        }
    }
}
