namespace Beresta.Markdown.Wpf.Kit;

using Markdig;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;

/// <summary>Вики-ссылки и встраивания для Markdig: разбор раньше обычных ссылок и HTML.</summary>
internal sealed class WikiMarkdigExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<WikiLinkParser>())
        {
            pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new WikiLinkParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html && !html.ObjectRenderers.Contains<WikiLinkHtmlRenderer>())
        {
            html.ObjectRenderers.Insert(0, new WikiLinkHtmlRenderer());
        }
    }
}
