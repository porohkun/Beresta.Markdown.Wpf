namespace MarkdigWpfViewer.ViewModels;

/// <summary>Цитата.</summary>
public sealed record QuoteVm(IReadOnlyList<BlockVm> Blocks)
    : BlockVm;