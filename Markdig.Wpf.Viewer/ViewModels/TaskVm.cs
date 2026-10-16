namespace MarkdigWpfViewer.ViewModels;

using Selection;

/// <summary>Флажок пункта задачи: <c>[ ]</c> или <c>[x]</c>.</summary>
public sealed record TaskVm(bool IsChecked) : InlineVm, IMdCopyText
{
    /// <summary>Знак статуса задачи от расширения: <c>/</c>, <c>-</c>, <c>&gt;</c>, <c>?</c>, <c>!</c>; у флажка — <c>null</c>.</summary>
    public char? Status { get; init; }

    public string CopyText => Status is { } status ? $"[{status}]" : IsChecked ? "[x]" : "[ ]";
}
