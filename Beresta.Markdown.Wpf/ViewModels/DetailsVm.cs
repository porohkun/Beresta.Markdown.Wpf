namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A collapsible block: a summary and nested blocks.</summary>
/// <param name="Summary">The summary; empty — the summary from the captions of the theme.</param>
/// <param name="Blocks">Nested blocks.</param>
public sealed record DetailsVm(IReadOnlyList<InlineVm> Summary, IReadOnlyList<BlockVm> Blocks)
    : BlockVm
{
    /// <summary>Expanded until it is collapsed.</summary>
    public bool IsOpen { get; init; }
}
