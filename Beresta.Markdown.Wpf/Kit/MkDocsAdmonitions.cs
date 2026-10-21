namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig;
using Markdig.Syntax;
using ViewModels;

/// <summary>
/// MkDocs admonitions: <c>!!! kind "Title"</c> and content indented by four spaces; <c>???</c> is a
/// collapsed and <c>???+</c> an expanded collapsible one; expanding and collapsing write the mark
/// back.
/// </summary>
public sealed class MkDocsAdmonitions : MdExtension
{
    /// <inheritdoc/>
    public override MdSourceEdit? EditSource(NodeVm node, object? state, MdSourceContext context)
    {
        if (node is not CalloutVm { IsFoldable: true } callout || state is not MdFoldState fold || callout.Source.IsEmpty)
        {
            return null;
        }

        var text = context.Text;
        var at = callout.Source.Start;
        while (at < text.Length && text[at] is ' ' or '\t')
        {
            at++;
        }

        if (at + 3 > text.Length || string.CompareOrdinal(text, at, "???", 0, 3) != 0)
        {
            return null;
        }

        var expanded = at + 3 < text.Length && text[at + 3] == '+';
        if (expanded == fold.IsExpanded)
        {
            return null;
        }

        return fold.IsExpanded ? new(at + 3, 0, "+") : new(at + 3, 1, string.Empty);
    }

    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<AdmonitionMarkdigExtension>();

    /// <inheritdoc/>
    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
    {
        if (block is not AdmonitionBlock admonition)
        {
            return null;
        }

        var heading = CalloutKinds.FirstLine(admonition, context).Span;
        IReadOnlyList<InlineVm> title = admonition.Title is { } text ? [new RunVm(text) { Source = heading }] : [];
        return CalloutKinds.Callout(admonition.Kind, title, context.ConvertChildren(admonition), heading) with
        {
            IsFoldable = admonition.IsCollapsible,
            IsFolded = admonition.IsCollapsible && !admonition.IsExpanded
        };
    }
}
