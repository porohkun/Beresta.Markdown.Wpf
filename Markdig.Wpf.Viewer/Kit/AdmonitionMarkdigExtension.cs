namespace MarkdigWpfViewer.Kit;

using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Предупреждения MkDocs для Markdig: разбор и HTML <c>&lt;div class="admonition вид"&gt;</c>.</summary>
internal sealed class AdmonitionMarkdigExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Contains<AdmonitionParser>())
        {
            pipeline.BlockParsers.InsertBefore<ParagraphBlockParser>(new AdmonitionParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html && !html.ObjectRenderers.Contains<AdmonitionHtmlRenderer>())
        {
            html.ObjectRenderers.Insert(0, new AdmonitionHtmlRenderer());
        }
    }
}
