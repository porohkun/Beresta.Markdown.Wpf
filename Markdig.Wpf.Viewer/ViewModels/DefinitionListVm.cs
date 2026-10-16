namespace MarkdigWpfViewer.ViewModels;

/// <summary>Список определений: термины и их определения.</summary>
public sealed record DefinitionListVm(IReadOnlyList<DefinitionItemVm> Items)
    : BlockVm;
