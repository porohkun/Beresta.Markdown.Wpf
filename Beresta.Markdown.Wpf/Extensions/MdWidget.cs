namespace Beresta.Markdown.Wpf.Extensions;

using System.Windows;

/// <summary>The state of a widget that lives longer than its control.</summary>
public static class MdWidget
{
    /// <summary>
    /// The state of a widget from <see cref="MdExtension.CreateState"/>; the whole template of the
    /// widget sees it. The viewer keeps it by <c>NodeId</c>, so it survives both virtualization and
    /// a new text.
    /// </summary>
    public static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached(
            "State",
            typeof(object),
            typeof(MdWidget),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Хранилище состояний вьюера; наследуется его содержимым.</summary>
    internal static readonly DependencyProperty StatesProperty =
        DependencyProperty.RegisterAttached(
            "States",
            typeof(WidgetStates),
            typeof(MdWidget),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    /// <inheritdoc cref="StateProperty"/>
    public static object? GetState(DependencyObject element) => element.GetValue(StateProperty);

    internal static void SetState(DependencyObject element, object? value) => element.SetValue(StateProperty, value);
}