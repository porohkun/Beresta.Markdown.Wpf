namespace MarkdigWpfViewer.Kit;

using System.Windows;
using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Статусы задач <c>- [/]</c>, <c>- [-]</c>, <c>- [&gt;]</c>, <c>- [?]</c>, <c>- [!]</c> — значком вместо флажка;
/// статус — в <see cref="TaskVm.Status"/>. Вид — шаблон темы <c>Md.TaskStatus.Template</c>.
/// </summary>
public sealed class TaskStatuses : MdExtension
{
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<TaskStatusMarkdigExtension>();

    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
        => inline is TaskStatusInline task ? new TaskVm(false) { Status = task.Status } : null;

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is TaskVm { Status: not null } && container is FrameworkElement element ? element.TryFindResource("Md.TaskStatus.Template") as DataTemplate : null;
}
