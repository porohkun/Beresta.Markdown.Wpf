namespace MarkdigWpfViewer.ViewModels;

using Abstractions;

public sealed record TableVm(int ColumnCount, IReadOnlyList<TableRowVm> Rows)
    : IMdBlockVm;