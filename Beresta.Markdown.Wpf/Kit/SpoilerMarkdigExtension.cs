namespace Beresta.Markdown.Wpf.Kit;

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Спойлеры для Markdig: разбор и HTML <c>&lt;span class="spoiler"&gt;</c>.</summary>
internal sealed class SpoilerMarkdigExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<SpoilerParser>())
        {
            pipeline.InlineParsers.Insert(0, new SpoilerParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html && !html.ObjectRenderers.Contains<SpoilerHtmlRenderer>())
        {
            html.ObjectRenderers.Insert(0, new SpoilerHtmlRenderer());
        }
    }
}
