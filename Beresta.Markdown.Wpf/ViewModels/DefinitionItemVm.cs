namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>
/// An item of a definition list: terms — each on its own line — and the blocks of the definition.
/// An item without terms is one more definition of the terms of the item before it.
/// </summary>
public sealed record DefinitionItemVm(IReadOnlyList<InlineVm> Terms, IReadOnlyList<BlockVm> Blocks)
    : NodeVm
{
    /// <summary>The item has its own terms.</summary>
    public bool HasTerms => Terms.Count > 0;
}
