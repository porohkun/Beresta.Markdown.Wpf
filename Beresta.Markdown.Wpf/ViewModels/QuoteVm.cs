namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A block quote.</summary>
public sealed record QuoteVm(IReadOnlyList<BlockVm> Blocks)
    : BlockVm;