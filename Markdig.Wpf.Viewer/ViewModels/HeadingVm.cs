namespace MarkdigWpfViewer.ViewModels;

using Abstractions;

public sealed record HeadingVm(int Level, IReadOnlyList<InlineVm> Inlines)
    : IMdBlockVm;