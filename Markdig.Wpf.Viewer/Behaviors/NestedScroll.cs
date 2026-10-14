namespace MarkdigWpfViewer.Behaviors;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

/// <summary>
/// Hands the mouse wheel over to the parent instead of letting a nested <see cref="ScrollViewer"/>
/// swallow it. A ScrollViewer with vertical scrolling disabled still marks the wheel as handled
/// (falling back to horizontal paging), which freezes the hosting page whenever the pointer sits
/// over a table or a code block. Shift + wheel keeps scrolling the nested viewer horizontally.
/// </summary>
public static class NestedScroll
{
    public static readonly DependencyProperty PassWheelToParentProperty =
        DependencyProperty.RegisterAttached(
            "PassWheelToParent",
            typeof(bool),
            typeof(NestedScroll),
            new PropertyMetadata(false, OnPassWheelToParentChanged));

    public static bool GetPassWheelToParent(DependencyObject element)
        => (bool)element.GetValue(PassWheelToParentProperty);

    public static void SetPassWheelToParent(DependencyObject element, bool value)
        => element.SetValue(PassWheelToParentProperty, value);

    private static void OnPassWheelToParentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
            return;

        scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;

        if (e.NewValue is true)
            scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scrollViewer = (ScrollViewer)sender;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && scrollViewer.ScrollableWidth > 0)
        {
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
            return;
        }

        if (VisualTreeHelper.GetParent(scrollViewer) is not UIElement parent)
            return;

        // Handling the tunnelling event keeps the ScrollViewer itself out of the way,
        // so the copy raised on the parent is free to bubble up to the outer scroller.
        e.Handled = true;

        parent.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = scrollViewer
        });
    }
}
