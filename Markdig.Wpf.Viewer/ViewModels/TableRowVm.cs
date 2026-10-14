namespace MarkdigWpfViewer.ViewModels;

using Abstractions;

public sealed record TableRowVm(bool IsHeader, IReadOnlyList<TableCellVm> Cells)
    : IMdBlockVm;