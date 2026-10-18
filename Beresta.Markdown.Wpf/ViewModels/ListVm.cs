namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A list; <paramref name="Depth"/> is the nesting among lists, counting from zero.</summary>
public sealed record ListVm(bool Ordered, int Start, int Depth, IReadOnlyList<ListItemVm> Items)
    : BlockVm;