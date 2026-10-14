namespace MarkdigWpfViewer.ViewModels;

using Abstractions;

public sealed record ListItemVm(string MarkerText, IReadOnlyList<IMdBlockVm> Blocks)
    : IMdBlockVm;