namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A callout: the kind, the title and nested blocks.</summary>
/// <param name="Kind">The kind in lowercase: note, tip, important, warning, caution or another one.</param>
/// <param name="Title">Its own title; empty — the title of the kind from the captions of the theme.</param>
/// <param name="Blocks">Nested blocks.</param>
public sealed record CalloutVm(string Kind, IReadOnlyList<InlineVm> Title, IReadOnlyList<BlockVm> Blocks)
    : BlockVm
{
    /// <summary>The callout can be folded by a click on its title.</summary>
    public bool IsFoldable { get; init; }

    /// <summary>A foldable callout is folded until it is unfolded.</summary>
    public bool IsFolded { get; init; }
}
