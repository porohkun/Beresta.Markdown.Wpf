namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>Таблица; ячейки каждой строки с объединёнными и пустыми добивками занимают <paramref name="ColumnCount"/> столбцов.</summary>
public sealed record TableVm(int ColumnCount, IReadOnlyList<TableRowVm> Rows)
    : BlockVm
{
    /// <summary>Выравнивание по столбцам; столбцу без своего — <see cref="TableAlignment.Default"/>.</summary>
    public IReadOnlyList<TableAlignment> Alignments { get; init; } = [];

    /// <summary>Выравнивание столбца; за пределами списка — <see cref="TableAlignment.Default"/>.</summary>
    public TableAlignment AlignmentOf(int column)
        => column < Alignments.Count ? Alignments[column] : TableAlignment.Default;
}