namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A definition list: terms and their definitions.</summary>
public sealed record DefinitionListVm(IReadOnlyList<DefinitionItemVm> Items)
    : BlockVm;
