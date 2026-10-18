namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Статус задачи в HTML — знаком в скобках, как в исходнике.</summary>
internal sealed class TaskStatusHtmlRenderer : HtmlObjectRenderer<TaskStatusInline>
{
    protected override void Write(HtmlRenderer renderer, TaskStatusInline task)
        => renderer.Write("[").WriteEscape(task.Status.ToString()).Write("]");
}
