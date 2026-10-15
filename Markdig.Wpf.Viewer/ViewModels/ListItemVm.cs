namespace MarkdigWpfViewer.ViewModels;

/// <summary>Пункт списка с готовым маркером.</summary>
public sealed record ListItemVm(string MarkerText, IReadOnlyList<BlockVm> Blocks)
    : NodeVm;