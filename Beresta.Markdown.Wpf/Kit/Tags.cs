namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Tags <c>#tag</c> and <c>#tag/nested</c> as a chip with the style <c>Tag</c> and the link
/// <c>tag:tag</c>. A hash inside a link is the text of the link.
/// </summary>
public sealed class Tags : MdExtension
{
    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<TagMarkdigExtension>();

    /// <inheritdoc/>
    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
    {
        if (inline is not TagInline tag)
        {
            return null;
        }

        var run = context.CreateRun("#" + tag.Name);
        if (run.Href is not null)
        {
            return [run];
        }

        var href = "tag:" + tag.Name;
        return
        [
            run with
            {
                Href = href,
                Hyperlink = KitLinks.UriOf(href),
                StyleName = "Tag",
                Markup = new(string.Empty, string.Empty) { ReplacesLink = true }
            }
        ];
    }
}
