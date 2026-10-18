namespace Beresta.Markdown.Wpf.Selection;

using System.Windows;

/// <summary>Attached properties of the selection.</summary>
public static class MdSelection
{
    /// <summary>
    /// The element handles the mouse itself: the selection does not start in it. Buttons, input
    /// fields and scroll bars are recognized without this property.
    /// </summary>
    public static readonly DependencyProperty IsInteractiveProperty =
        DependencyProperty.RegisterAttached(
            "IsInteractive",
            typeof(bool),
            typeof(MdSelection),
            new(false));

    /// <summary>Что сейчас подсвечено в документе; ставит вьюер, наследуют его презентеры.</summary>
    internal static readonly DependencyProperty LayerProperty =
        DependencyProperty.RegisterAttached(
            "Layer",
            typeof(MdDecorationLayer),
            typeof(MdSelection),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    /// <inheritdoc cref="IsInteractiveProperty"/>
    public static bool GetIsInteractive(DependencyObject element)
        => (bool)element.GetValue(IsInteractiveProperty);

    /// <inheritdoc cref="IsInteractiveProperty"/>
    public static void SetIsInteractive(DependencyObject element, bool value)
        => element.SetValue(IsInteractiveProperty, value);
}