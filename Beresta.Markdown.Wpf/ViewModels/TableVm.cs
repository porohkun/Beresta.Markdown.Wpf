namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>
/// A table; the cells of every row, with merged and empty fillers, take
/// <paramref name="ColumnCount"/> columns.
/// </summary>
public sealed record TableVm(int ColumnCount, IReadOnlyList<TableRowVm> Rows)
    : BlockVm
{
    /// <summary>Alignment by columns; a column without its own has <see cref="TableAlignment.Default"/>.</summary>
    public IReadOnlyList<TableAlignment> Alignments { get; init; } = [];

    /// <summary>The alignment of a column; beyond the list — <see cref="TableAlignment.Default"/>.</summary>
    public TableAlignment AlignmentOf(int column)
        => column < Alignments.Count ? Alignments[column] : TableAlignment.Default;
}