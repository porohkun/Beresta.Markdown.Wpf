namespace Beresta.Markdown.Wpf.Kit;

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Шорткоды для Markdig: разбор и HTML — исходным текстом шорткода.</summary>
internal sealed class ShortcodeMarkdigExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<ShortcodeParser>())
        {
            pipeline.InlineParsers.Insert(0, new ShortcodeParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html && !html.ObjectRenderers.Contains<ShortcodeHtmlRenderer>())
        {
            html.ObjectRenderers.Insert(0, new ShortcodeHtmlRenderer());
        }
    }
}
