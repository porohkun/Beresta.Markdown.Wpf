namespace MarkdigWpfViewer;

using System.Windows;
using System.Windows.Input;

/// <summary>
/// Точка перехвата ссылок разметки. Нужна тем, у кого ссылка ведёт не наружу, а внутрь
/// приложения: <c>[подпись](своя_схема:идентификатор)</c>.
///
/// <para>
/// Подключиться можно двумя способами, и оба ставятся на любого предка вьюера — событие
/// всплывает: обработчиком (<c>MarkdownLink.Navigate="..."</c>) либо командой
/// (<c>MarkdownLink.Command="{Binding ...}"</c>), которая получает
/// <see cref="MarkdownLinkEventArgs.Href"/> параметром.
/// </para>
///
/// <para>
/// Необработанная ссылка остаётся ссылкой наружу: вьюер открывает абсолютный адрес системным
/// обработчиком. Поэтому команда, отказавшаяся исполняться (<c>CanExecute</c> вернул
/// <c>false</c>), событие не гасит — «не моя ссылка» и «моя, но сейчас нельзя» здесь одно и то
/// же, а внешние ссылки в тексте обязаны продолжать работать.
/// </para>
/// </summary>
public static class MarkdownLink
{
    /// <summary>Ссылку нажали. Всплывающее: обработчик ставится на любого предка вьюера.</summary>
    public static readonly RoutedEvent NavigateEvent =
        EventManager.RegisterRoutedEvent(
            "Navigate",
            RoutingStrategy.Bubble,
            typeof(MarkdownLinkEventHandler),
            typeof(MarkdownLink));

    /// <summary>
    /// Команда, которой достаётся нажатая ссылка; параметр — <see cref="MarkdownLinkEventArgs.Href"/>.
    /// </summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(MarkdownLink),
            new(null, OnCommandChanged));

    public static void AddNavigateHandler(DependencyObject element, MarkdownLinkEventHandler handler) =>
        (element as UIElement)?.AddHandler(NavigateEvent, handler);

    public static void RemoveNavigateHandler(DependencyObject element, MarkdownLinkEventHandler handler) =>
        (element as UIElement)?.RemoveHandler(NavigateEvent, handler);

    public static ICommand? GetCommand(DependencyObject element) =>
        (ICommand?)element.GetValue(CommandProperty);

    public static void SetCommand(DependencyObject element, ICommand? value) =>
        element.SetValue(CommandProperty, value);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;

        // Подписка ставится и снимается один раз — обработчик статический и команду читает по
        // месту, поэтому смена самой команды переподписки не требует.
        switch (e.OldValue, e.NewValue)
        {
            case (null, not null):
                element.AddHandler(NavigateEvent, new MarkdownLinkEventHandler(OnNavigate));
                break;
            case (not null, null):
                element.RemoveHandler(NavigateEvent, new MarkdownLinkEventHandler(OnNavigate));
                break;
        }
    }

    private static void OnNavigate(object sender, MarkdownLinkEventArgs e)
    {
        if (sender is not DependencyObject element || GetCommand(element) is not { } command)
            return;

        if (!command.CanExecute(e.Href))
            return;

        command.Execute(e.Href);
        e.Handled = true;
    }
}
