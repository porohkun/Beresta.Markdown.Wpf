namespace MarkdigWpfViewer.Kit;

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Упоминания для Markdig: разбор и HTML — ссылкой <c>mention:</c>.</summary>
internal sealed class MentionMarkdigExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<MentionParser>())
        {
            pipeline.InlineParsers.Insert(0, new MentionParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html && !html.ObjectRenderers.Contains<MentionHtmlRenderer>())
        {
            html.ObjectRenderers.Insert(0, new MentionHtmlRenderer());
        }
    }
}
