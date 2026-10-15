namespace MarkdigWpfViewer.ViewModels;

/// <summary>Таблица; строки дополнены пустыми ячейками до <paramref name="ColumnCount"/>.</summary>
public sealed record TableVm(int ColumnCount, IReadOnlyList<TableRowVm> Rows)
    : BlockVm;
