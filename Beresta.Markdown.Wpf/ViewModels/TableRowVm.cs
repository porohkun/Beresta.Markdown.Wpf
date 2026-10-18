namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A table row.</summary>
public sealed record TableRowVm(bool IsHeader, IReadOnlyList<TableCellVm> Cells)
    : NodeVm;