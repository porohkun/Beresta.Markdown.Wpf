namespace MarkdigWpfViewer.ViewModels;

using Abstractions;

public sealed record TableCellVm(IReadOnlyList<InlineVm> Inlines)
    : IMdBlockVm;