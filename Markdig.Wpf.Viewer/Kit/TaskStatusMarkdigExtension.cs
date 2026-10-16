namespace MarkdigWpfViewer.Kit;

using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Статусы задач для Markdig: разбор и HTML — знаком статуса в скобках.</summary>
internal sealed class TaskStatusMarkdigExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<TaskStatusParser>())
        {
            pipeline.InlineParsers.Insert(0, new TaskStatusParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html && !html.ObjectRenderers.Contains<TaskStatusHtmlRenderer>())
        {
            html.ObjectRenderers.Insert(0, new TaskStatusHtmlRenderer());
        }
    }
}
