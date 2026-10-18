namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A piece of text with one set of styles.</summary>
/// <param name="Text">The text of the piece.</param>
/// <param name="Bold">Bold, <c>**text**</c>, <c>&lt;b&gt;</c> or <c>&lt;strong&gt;</c>.</param>
/// <param name="Italic">Italic, <c>*text*</c>, <c>&lt;i&gt;</c> or <c>&lt;em&gt;</c>.</param>
/// <param name="Strike">Strikethrough, <c>~~text~~</c>, <c>&lt;s&gt;</c>, <c>&lt;del&gt;</c> or
/// <c>&lt;strike&gt;</c>.</param>
/// <param name="Code">Inline code, <c>`code`</c> or <c>&lt;code&gt;</c>.</param>
/// <param name="Hyperlink">The parsed <paramref name="Href"/>; no link — <c>null</c>.</param>
/// <param name="Href">The target of the link; no link — <c>null</c>.</param>
/// <param name="LinkTitle">The title of the link (<c>[text](address "title")</c>); none — <c>null</c>.</param>
/// <param name="Kbd">A key, <c>&lt;kbd&gt;</c>.</param>
/// <param name="Subscript">Subscript, <c>~text~</c> or <c>&lt;sub&gt;</c>.</param>
/// <param name="Superscript">Superscript, <c>^text^</c> or <c>&lt;sup&gt;</c>.</param>
/// <param name="Marked">Highlighted with a marker, <c>==text==</c> or <c>&lt;mark&gt;</c>.</param>
/// <param name="Inserted">Inserted text, <c>++text++</c>, <c>&lt;ins&gt;</c> or <c>&lt;u&gt;</c>.</param>
/// <param name="Cite">The title of a work, <c>""text""</c> or <c>&lt;cite&gt;</c>.</param>
/// <param name="Abbreviation">The expansion of an abbreviation from <c>*[label]: expansion</c>;
/// none — <c>null</c>.</param>
public sealed record RunVm(
    string Text,
    bool Bold = false,
    bool Italic = false,
    bool Strike = false,
    bool Code = false,
    Uri? Hyperlink = null,
    string? Href = null,
    string? LinkTitle = null,
    bool Kbd = false,
    bool Subscript = false,
    bool Superscript = false,
    bool Marked = false,
    bool Inserted = false,
    bool Cite = false,
    string? Abbreviation = null)
    : InlineVm
{
    /// <summary>
    /// The name of a look: the piece is drawn with the <c>Span</c> style under the theme key
    /// <c>Md.&lt;name&gt;.Style</c>; none — <c>null</c>.
    /// </summary>
    public string? StyleName { get; init; }

    /// <summary>
    /// The framing of the piece in the markup of the selection; none — the piece is written without
    /// framing.
    /// </summary>
    public MarkupFrame? Markup { get; init; }
}