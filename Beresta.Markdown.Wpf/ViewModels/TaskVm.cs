namespace Beresta.Markdown.Wpf.ViewModels;

using Selection;

/// <summary>The checkbox of a task item: <c>[ ]</c> or <c>[x]</c>.</summary>
public sealed record TaskVm(bool IsChecked) : InlineVm, IMdCopyText
{
    /// <summary>
    /// The status character of the task from an extension: <c>/</c>, <c>-</c>, <c>&gt;</c>,
    /// <c>?</c>, <c>!</c>; a checkbox has <c>null</c>.
    /// </summary>
    public char? Status { get; init; }

    /// <inheritdoc/>
    public string CopyText => Status is { } status ? $"[{status}]" : IsChecked ? "[x]" : "[ ]";
}