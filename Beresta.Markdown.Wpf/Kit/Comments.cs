namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>Комментарии <c>%%…%%</c> — внутри строки и блоком на несколько строк; не рисуются.</summary>
public sealed class Comments : MdExtension
{
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<CommentMarkdigExtension>();

    public override IReadOnlyList<BlockVm>? ConvertBlocks(Block block, MdConvertContext context)
        => block is CommentBlock ? [] : null;

    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => inline is CommentInline ? [] : null;
}
