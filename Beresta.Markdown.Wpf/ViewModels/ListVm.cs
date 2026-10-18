namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>Список; <paramref name="Depth"/> — вложенность среди списков, считая с нуля.</summary>
public sealed record ListVm(bool Ordered, int Start, int Depth, IReadOnlyList<ListItemVm> Items)
    : BlockVm;