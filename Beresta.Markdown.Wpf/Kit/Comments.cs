namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>Comments <c>%%…%%</c> — inside a line and as a block of several lines; not drawn.</summary>
public sealed class Comments : MdExtension
{
    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<CommentMarkdigExtension>();

    /// <inheritdoc/>
    public override IReadOnlyList<BlockVm>? ConvertBlocks(Block block, MdConvertContext context)
        => block is CommentBlock ? [] : null;

    /// <inheritdoc/>
    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => inline is CommentInline ? [] : null;
}
