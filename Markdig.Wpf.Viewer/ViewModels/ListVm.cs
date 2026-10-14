namespace MarkdigWpfViewer.ViewModels;

using Abstractions;

public sealed record ListVm(bool Ordered, int Start, int Depth, IReadOnlyList<ListItemVm> Items)
    : IMdBlockVm;