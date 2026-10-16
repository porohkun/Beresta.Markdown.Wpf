namespace MarkdigWpfViewer.ViewModels;

using Selection;

/// <summary>Флажок пункта задачи: <c>[ ]</c> или <c>[x]</c>.</summary>
public sealed record TaskVm(bool IsChecked) : InlineVm, IMdCopyText
{
    public string CopyText => IsChecked ? "[x]" : "[ ]";
}
