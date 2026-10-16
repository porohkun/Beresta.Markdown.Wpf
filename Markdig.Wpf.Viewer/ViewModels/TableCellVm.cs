namespace MarkdigWpfViewer.ViewModels;

/// <summary>Ячейка таблицы.</summary>
public sealed record TableCellVm(IReadOnlyList<InlineVm> Inlines)
    : NodeVm;