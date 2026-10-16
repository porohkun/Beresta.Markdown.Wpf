namespace MarkdigWpfViewer.Extensions;

using System.Windows;

/// <summary>Состояние виджета, которое живёт дольше его контрола.</summary>
public static class MdWidget
{
    /// <summary>
    /// Состояние виджета от <see cref="MdExtension.CreateState"/>; его видит весь шаблон виджета.
    /// Вьюер хранит состояние по <c>NodeId</c>, так что оно переживает и виртуализацию, и новый текст.
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

    public static object? GetState(DependencyObject element) => element.GetValue(StateProperty);

    internal static void SetState(DependencyObject element, object? value) => element.SetValue(StateProperty, value);
}