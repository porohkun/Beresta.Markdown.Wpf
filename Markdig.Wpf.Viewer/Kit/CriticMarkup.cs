namespace MarkdigWpfViewer.Kit;

using System.Windows;
using System.Windows.Input;
using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// CriticMarkup: <c>{++вставка++}</c>, <c>{--удаление--}</c>, <c>{~~было~&gt;стало~~}</c>,
/// <c>{==выделение==}</c> — оформленным текстом <c>Critic.Insertion</c>, <c>Critic.Deletion</c>,
/// <c>Critic.Highlight</c>; <c>{&gt;&gt;комментарий&lt;&lt;}</c> — значком с подсказкой.
/// </summary>
public sealed class CriticMarkup : MdExtension
{
    /// <summary>Правку приняли или отклонили. Всплывающее: обработчик ставится на вьюер или любого его предка.</summary>
    public static readonly RoutedEvent DecidedEvent =
        EventManager.RegisterRoutedEvent(
            "Decided",
            RoutingStrategy.Bubble,
            typeof(EventHandler<MdCriticDecidedEventArgs>),
            typeof(CriticMarkup));

    /// <summary>Принять правку; параметр — <see cref="CriticDecisionVm"/>.</summary>
    public static readonly RoutedCommand AcceptCommand = new(nameof(AcceptCommand), typeof(CriticMarkup));

    /// <summary>Отклонить правку; параметр — <see cref="CriticDecisionVm"/>.</summary>
    public static readonly RoutedCommand RejectCommand = new(nameof(RejectCommand), typeof(CriticMarkup));

    static CriticMarkup()
    {
        CommandManager.RegisterClassCommandBinding(typeof(MarkdownViewer), new(AcceptCommand, (_, e) => Decide(e, isAccepted: true), CanDecide));
        CommandManager.RegisterClassCommandBinding(typeof(MarkdownViewer), new(RejectCommand, (_, e) => Decide(e, isAccepted: false), CanDecide));
    }

    /// <summary>Показывать у вставок, удалений и замен кнопки «принять» и «отклонить», которые поднимают <see cref="DecidedEvent"/>.</summary>
    public bool AllowDecisions { get; set; }

    public static void AddDecidedHandler(DependencyObject element, EventHandler<MdCriticDecidedEventArgs> handler) =>
        (element as UIElement)?.AddHandler(DecidedEvent, handler);

    public static void RemoveDecidedHandler(DependencyObject element, EventHandler<MdCriticDecidedEventArgs> handler) =>
        (element as UIElement)?.RemoveHandler(DecidedEvent, handler);

    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<CriticMarkdigExtension>();

    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
    {
        if (inline is not CriticInline critic)
        {
            return null;
        }

        List<InlineVm> parts = critic.Kind switch
        {
            CriticKind.Insertion => [Styled(critic.Text, "Critic.Insertion", "{++", "++}")],
            CriticKind.Deletion => [Styled(critic.Text, "Critic.Deletion", "{--", "--}")],
            CriticKind.Substitution =>
            [
                Styled(critic.Text, "Critic.Deletion", "{~~", "~>"),
                Styled(critic.Replacement ?? string.Empty, "Critic.Insertion", string.Empty, "~~}")
            ],
            CriticKind.Highlight => [Styled(critic.Text, "Critic.Highlight", "{==", "==}")],
            _ => [new CriticCommentVm(critic.Text)]
        };

        if (AllowDecisions && critic.Kind is CriticKind.Insertion or CriticKind.Deletion or CriticKind.Substitution)
        {
            var (accepted, rejected) = critic.Kind switch
            {
                CriticKind.Insertion => (critic.Text, string.Empty),
                CriticKind.Deletion => (string.Empty, critic.Text),
                _ => (critic.Replacement ?? string.Empty, critic.Text)
            };
            parts.Add(new CriticDecisionVm(context.GetSpan(critic), accepted, rejected));
        }

        return parts;

        RunVm Styled(string text, string style, string open, string close)
            => context.CreateRun(text) with { StyleName = style, Markup = new(open, close) };
    }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        var key = item switch
        {
            CriticCommentVm => "Md.Critic.Comment.Template",
            CriticDecisionVm => "Md.Critic.Decision.Template",
            _ => null
        };

        return key is not null && container is FrameworkElement element ? element.TryFindResource(key) as DataTemplate : null;
    }

    private static void CanDecide(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = e.Parameter is CriticDecisionVm;

    private static void Decide(ExecutedRoutedEventArgs e, bool isAccepted)
    {
        if (e.Parameter is CriticDecisionVm decision && e.OriginalSource is UIElement source)
        {
            source.RaiseEvent(new MdCriticDecidedEventArgs(DecidedEvent, source, decision.Change, isAccepted ? decision.Accepted : decision.Rejected, isAccepted));
        }
    }
}
