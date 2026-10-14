namespace MarkdigWpfViewer.ViewModels;

using Abstractions;

public sealed record QuoteVm(IReadOnlyList<IMdBlockVm> Blocks)
    : IMdBlockVm;