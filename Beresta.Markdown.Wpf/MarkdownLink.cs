namespace Beresta.Markdown.Wpf;

using System.Windows;
using System.Windows.Input;

/// <summary>
/// The place where links of the markup are intercepted: for links that lead into the application,
/// <c>[caption](your_scheme:id)</c>, rather than out of it.
/// </summary>
/// <remarks>
/// Hook in on any ancestor of the viewer, since the event bubbles: with a handler,
/// <c>MarkdownLink.Navigate="..."</c>, or with a command, <c>MarkdownLink.Command="{Binding
/// ...}"</c>, which gets <see cref="MarkdownLinkEventArgs.Href"/> as its parameter. An unhandled
/// link to an anchor of the document, <c>#id</c>, scrolls the viewer to the anchor; the rest goes
/// to the opener in <see cref="OpenerProperty"/>. A command that refuses to execute does not stop
/// the event.
/// </remarks>
public static class MarkdownLink
{
    /// <summary>A link was clicked. Bubbles: set a handler on any ancestor of the viewer.</summary>
    public static readonly RoutedEvent NavigateEvent =
        EventManager.RegisterRoutedEvent(
            "Navigate",
            RoutingStrategy.Bubble,
            typeof(MarkdownLinkEventHandler),
            typeof(MarkdownLink));

    /// <summary>
    /// Who opens a link that the application did not handle; inherited — set it on the viewer, the
    /// window or in a style. By default <see cref="MdSystemLinkOpener"/>; <c>null</c> — nobody
    /// opens links.
    /// </summary>
    public static readonly DependencyProperty OpenerProperty =
        DependencyProperty.RegisterAttached(
            "Opener",
            typeof(IMdLinkOpener),
            typeof(MarkdownLink),
            new FrameworkPropertyMetadata(MdSystemLinkOpener.Instance, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>
    /// A click on a link from the template of your widget; the parameter is the target of the link
    /// as a string. Goes the same way as a link in the text; executed by the viewer that holds the
    /// widget.
    /// </summary>
    public static readonly RoutedCommand ActivateCommand = new(nameof(ActivateCommand), typeof(MarkdownLink));

    /// <summary>
    /// A command that gets a clicked link; the parameter is
    /// <see cref="MarkdownLinkEventArgs.Href"/>.
    /// </summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(MarkdownLink),
            new(null, OnCommandChanged));

    /// <summary>Adds a handler of <see cref="NavigateEvent"/> to an element.</summary>
    public static void AddNavigateHandler(DependencyObject element, MarkdownLinkEventHandler handler) =>
        (element as UIElement)?.AddHandler(NavigateEvent, handler);

    /// <summary>Removes a handler of <see cref="NavigateEvent"/> from an element.</summary>
    public static void RemoveNavigateHandler(DependencyObject element, MarkdownLinkEventHandler handler) =>
        (element as UIElement)?.RemoveHandler(NavigateEvent, handler);

    /// <inheritdoc cref="CommandProperty"/>
    public static ICommand? GetCommand(DependencyObject element) =>
        (ICommand?)element.GetValue(CommandProperty);

    /// <inheritdoc cref="CommandProperty"/>
    public static void SetCommand(DependencyObject element, ICommand? value) =>
        element.SetValue(CommandProperty, value);

    /// <inheritdoc cref="OpenerProperty"/>
    public static IMdLinkOpener? GetOpener(DependencyObject element) =>
        (IMdLinkOpener?)element.GetValue(OpenerProperty);

    /// <inheritdoc cref="OpenerProperty"/>
    public static void SetOpener(DependencyObject element, IMdLinkOpener? value) =>
        element.SetValue(OpenerProperty, value);

    /// <summary>A click on a link from your widget: goes the same way as a link in the text.</summary>
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