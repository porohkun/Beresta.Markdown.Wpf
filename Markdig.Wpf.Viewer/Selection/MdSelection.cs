namespace MarkdigWpfViewer.Selection;

using System.Windows;

/// <summary>Вложенные свойства выделения.</summary>
public static class MdSelection
{
    /// <summary>
    /// Элемент сам обрабатывает мышь: выделение в нём не начинается. Кнопки, поля ввода, полосы
    /// прокрутки распознаются и без этого свойства.
    /// </summary>
    public static readonly DependencyProperty IsInteractiveProperty =
        DependencyProperty.RegisterAttached(
            "IsInteractive",
            typeof(bool),
            typeof(MdSelection),
            new PropertyMetadata(false));

    public static bool GetIsInteractive(DependencyObject element)
        => (bool)element.GetValue(IsInteractiveProperty);

    public static void SetIsInteractive(DependencyObject element, bool value)
        => element.SetValue(IsInteractiveProperty, value);

    /// <summary>Что сейчас подсвечено в документе; ставит вьюер, наследуют его презентеры.</summary>
    internal static readonly DependencyProperty LayerProperty =
        DependencyProperty.RegisterAttached(
            "Layer",
            typeof(MdDecorationLayer),
            typeof(MdSelection),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
}
