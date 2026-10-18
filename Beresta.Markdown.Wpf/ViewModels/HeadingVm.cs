namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>
/// A heading of level 1 to 6; <paramref name="Id"/> is the identifier from Markdig, explicit
/// (<c>{#id}</c>) or generated.
/// </summary>
public sealed record HeadingVm(int Level, IReadOnlyList<InlineVm> Inlines, string? Id = null)
    : BlockVm;