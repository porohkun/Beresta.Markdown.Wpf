namespace Beresta.Markdown.Wpf.Kit;

using System.Windows;
using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Mentions <c>@name</c> as a chip with the link <c>mention:name</c>; the look is the theme
/// template <c>Md.Mention.Template</c>. The display name comes from <see cref="Resolver"/>.
/// <c>@</c> inside a link is the text of the link.
/// </summary>
public sealed class Mentions : MdExtension
{
    /// <summary>Knows the display names of people; none — the name as written.</summary>
    public IMdLinkResolver? Resolver { get; set; }

    /// <inheritdoc/>
    public override bool AllowsBackgroundParse => Resolver is null or IMdBackgroundSafe;

    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<MentionMarkdigExtension>();

    /// <inheritdoc/>
    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
    {
        if (inline is not MentionInline mention)
        {
            return null;
        }

        var run = context.CreateRun("@" + mention.Name);
        return run.Href is not null
            ? [run]
            : [new MentionVm(mention.Name, Resolver?.DisplayName(mention.Name) ?? mention.Name)];
    }

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is MentionVm && container is FrameworkElement element ? element.TryFindResource("Md.Mention.Template") as DataTemplate : null;
}
