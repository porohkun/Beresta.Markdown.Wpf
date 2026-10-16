namespace MarkdigWpfViewer.Kit;

using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

/// <summary>Комментарии <c>%%</c> для Markdig: разбор внутри строки и блоком; в HTML их нет.</summary>
internal sealed class CommentMarkdigExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Contains<CommentBlockParser>())
        {
            pipeline.BlockParsers.InsertBefore<ParagraphBlockParser>(new CommentBlockParser());
        }

        if (!pipeline.InlineParsers.Contains<CommentParser>())
        {
            pipeline.InlineParsers.Insert(0, new CommentParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html && !html.ObjectRenderers.Contains<Silent<CommentInline>>())
        {
            html.ObjectRenderers.Insert(0, new Silent<CommentInline>());
            html.ObjectRenderers.Insert(0, new Silent<CommentBlock>());
        }
    }

    /// <summary>Ничего не пишет в HTML.</summary>
    private sealed class Silent<T> : HtmlObjectRenderer<T>
        where T : MarkdownObject
    {
        protected override void Write(HtmlRenderer renderer, T obj)
        {
        }
    }
}
