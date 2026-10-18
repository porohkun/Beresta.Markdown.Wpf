namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>Цитата.</summary>
public sealed record QuoteVm(IReadOnlyList<BlockVm> Blocks)
    : BlockVm;