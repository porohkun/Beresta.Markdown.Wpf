namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Syntax.Inlines;

/// <summary>Статус задачи <c>[/]</c>, <c>[-]</c>, <c>[&gt;]</c>, <c>[?]</c>, <c>[!]</c> в начале пункта списка.</summary>
internal sealed class TaskStatusInline : LeafInline
{
    public required char Status { get; init; }
}
