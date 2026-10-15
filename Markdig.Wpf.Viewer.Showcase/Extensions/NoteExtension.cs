namespace MarkdigWpfViewer.Showcase.Extensions;

using System.Windows;
using Markdig.Extensions.CustomContainers;
using Markdig.Syntax;
using MarkdigWpfViewer.Extensions;
using ViewModels;

/// <summary><c>:::note Заголовок</c> — заметка со своей разметкой внутри.</summary>
public sealed class NoteExtension : MdExtension
{
    /// <summary>Шаблон заметки; данные — <see cref="NoteVm"/>.</summary>
    public DataTemplate? Template { get; set; }

    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
    {
        return block is CustomContainer { Info: "note" } container
            ? new NoteVm(container.Arguments ?? string.Empty, context.ConvertChildren(container))
            : null;
    }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is NoteVm ? Template : null;

    public override object? CreateState(NodeVm node)
        => node is NoteVm ? new NoteState() : null;
}
