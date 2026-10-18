namespace Beresta.Markdown.Wpf.Kit;

using System.Windows;
using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Task statuses <c>- [/]</c>, <c>- [-]</c>, <c>- [&gt;]</c>, <c>- [?]</c>, <c>- [!]</c> as an icon
/// instead of a box; the status is in <see cref="TaskVm.Status"/>. The look is the theme template
/// <c>Md.TaskStatus.Template</c>.
/// </summary>
public sealed class TaskStatuses : MdExtension
{
    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<TaskStatusMarkdigExtension>();

    /// <inheritdoc/>
    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
        => inline is TaskStatusInline task ? new TaskVm(false) { Status = task.Status } : null;

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is TaskVm { Status: not null } && container is FrameworkElement element ? element.TryFindResource("Md.TaskStatus.Template") as DataTemplate : null;
}
