namespace Beresta.Markdown.Wpf.Kit;

using System.Windows;
using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// CriticMarkup: <c>{++insertion++}</c>, <c>{--deletion--}</c>, <c>{~~before~&gt;after~~}</c>,
/// <c>{==highlight==}</c> as styled text <c>Critic.Insertion</c>, <c>Critic.Deletion</c>,
/// <c>Critic.Highlight</c>; <c>{&gt;&gt;comment&lt;&lt;}</c> as an icon with a tooltip.
/// </summary>
public sealed class CriticMarkup : MdExtension
{
    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<CriticMarkdigExtension>();

    /// <inheritdoc/>
    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
    {
        if (inline is not CriticInline critic)
        {
            return null;
        }

        return critic.Kind switch
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

        RunVm Styled(string text, string style, string open, string close)
            => context.CreateRun(text) with { StyleName = style, Markup = new(open, close) };
    }

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is CriticCommentVm && container is FrameworkElement element
            ? element.TryFindResource("Md.Critic.Comment.Template") as DataTemplate
            : null;
}
