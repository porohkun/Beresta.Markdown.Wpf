namespace MarkdigWpfViewer.ViewModels;

/// <summary>
/// Пункт списка определений: термины — каждый со своей строки — и блоки определения. Пункт без
/// терминов — ещё одно определение терминов пункта перед ним.
/// </summary>
public sealed record DefinitionItemVm(IReadOnlyList<InlineVm> Terms, IReadOnlyList<BlockVm> Blocks)
    : NodeVm
{
    /// <summary>У пункта есть свои термины.</summary>
    public bool HasTerms => Terms.Count > 0;
}
