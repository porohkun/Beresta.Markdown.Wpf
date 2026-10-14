namespace MarkdigWpfViewer.ViewModels;

using Abstractions;

public sealed record ParagraphVm(IReadOnlyList<InlineVm> Inlines)
    : IMdBlockVm;