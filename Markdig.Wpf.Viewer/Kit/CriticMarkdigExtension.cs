namespace MarkdigWpfViewer.Kit;

using Markdig;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;

/// <summary>CriticMarkup для Markdig: разбор и HTML.</summary>
internal sealed class CriticMarkdigExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<CriticParser>())
        {
            // BYDESIGN: раньше выделений — иначе они забирают ++, ~~ и == правок.
            pipeline.InlineParsers.InsertBefore<EmphasisInlineParser>(new CriticParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html && !html.ObjectRenderers.Contains<CriticHtmlRenderer>())
        {
            html.ObjectRenderers.Insert(0, new CriticHtmlRenderer());
        }
    }
}
