namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>
/// The framing of a piece of text in the markup of the selection: what is written before the text
/// and what after it.
/// </summary>
public sealed record MarkupFrame(string Open, string Close)
{
    /// <summary>The framing writes the link of the piece itself: no link markup is put around it.</summary>
    public bool ReplacesLink { get; init; }
}
