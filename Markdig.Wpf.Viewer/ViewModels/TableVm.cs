namespace MarkdigWpfViewer.ViewModels;

/// <summary>Таблица; строки дополнены пустыми ячейками до <paramref name="ColumnCount"/>.</summary>
public sealed record TableVm(int ColumnCount, IReadOnlyList<TableRowVm> Rows)
    : BlockVm
{
    /// <summary>Выравнивание по столбцам; столбцу без своего — <see cref="TableAlignment.Default"/>.</summary>
    public IReadOnlyList<TableAlignment> Alignments { get; init; } = [];

    /// <summary>Выравнивание столбца; за пределами списка — <see cref="TableAlignment.Default"/>.</summary>
    public TableAlignment AlignmentOf(int column)
        => column < Alignments.Count ? Alignments[column] : TableAlignment.Default;
}