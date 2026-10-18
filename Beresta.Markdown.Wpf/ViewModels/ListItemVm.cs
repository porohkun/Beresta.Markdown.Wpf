namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A list item with a ready marker.</summary>
public sealed record ListItemVm(string MarkerText, IReadOnlyList<BlockVm> Blocks)
    : NodeVm;