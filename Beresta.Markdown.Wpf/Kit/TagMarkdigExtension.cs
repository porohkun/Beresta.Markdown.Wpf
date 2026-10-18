namespace Beresta.Markdown.Wpf.Kit;

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Теги для Markdig: разбор и HTML — ссылкой <c>tag:</c>.</summary>
internal sealed class TagMarkdigExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<TagParser>())
        {
            pipeline.InlineParsers.Insert(0, new TagParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html && !html.ObjectRenderers.Contains<TagHtmlRenderer>())
        {
            html.ObjectRenderers.Insert(0, new TagHtmlRenderer());
        }
    }
}
