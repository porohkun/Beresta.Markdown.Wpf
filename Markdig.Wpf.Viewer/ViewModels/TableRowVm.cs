namespace MarkdigWpfViewer.ViewModels;

/// <summary>Строка таблицы.</summary>
public sealed record TableRowVm(bool IsHeader, IReadOnlyList<TableCellVm> Cells)
    : NodeVm;
