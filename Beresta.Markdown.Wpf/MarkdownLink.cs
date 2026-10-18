namespace Beresta.Markdown.Wpf;

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
/// Необработанная ссылка на якорь документа (<c>#id</c>) прокручивает вьюер к якорю, остальное
/// получает открыватель <see cref="OpenerProperty"/>. Поэтому команда, отказавшаяся исполняться
/// (<c>CanExecute</c> вернул <c>false</c>), событие не гасит — «не моя ссылка» и «моя, но сейчас
/// нельзя» здесь одно и то же, а внешние ссылки в тексте обязаны продолжать работать.
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
    /// Кто открывает ссылку, которую не обработало приложение; наследуется — ставится на вьюер, на
    /// окно или стилем. По умолчанию <see cref="MdSystemLinkOpener"/>, <c>null</c> — не открывает никто.
    /// </summary>
    public static readonly DependencyProperty OpenerProperty =
        DependencyProperty.RegisterAttached(
            "Opener",
            typeof(IMdLinkOpener),
            typeof(MarkdownLink),
            new FrameworkPropertyMetadata(MdSystemLinkOpener.Instance, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>
    /// Нажатие ссылки из шаблона своего виджета, параметр — цель ссылки строкой: тот же путь, что у
    /// ссылки в тексте. Исполняет вьюер, в котором лежит виджет.
    /// </summary>
    public static readonly RoutedCommand ActivateCommand = new(nameof(ActivateCommand), typeof(MarkdownLink));

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

    public static IMdLinkOpener? GetOpener(DependencyObject element) =>
        (IMdLinkOpener?)element.GetValue(OpenerProperty);

    public static void SetOpener(DependencyObject element, IMdLinkOpener? value) =>
        element.SetValue(OpenerProperty, value);

    /// <summary>Нажатие ссылки из своего виджета: тот же путь, что у ссылки в тексте.</summary>
    public static void Activate(UIElement source, string href)
        => Activate(source, href, Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var uri) ? uri : null);

    /// <summary>Нажатие ссылки: сначала событие приложению, затем якорь документа, затем открыватель.</summary>
    internal static void Activate(UIElement source, string href, Uri? uri)
    {
        var args = new MarkdownLinkEventArgs(NavigateEvent, source, href, uri);
        source.RaiseEvent(args);
        if (args.Handled)
            return;

        if (href.StartsWith('#') && MarkdownViewer.Owning(source) is { } viewer && viewer.ScrollToAnchor(href[1..]))
            return;

        GetOpener(source)?.Open(href, uri);
    }

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