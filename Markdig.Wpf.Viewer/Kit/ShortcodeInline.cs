namespace MarkdigWpfViewer.Kit;

using Markdig.Syntax.Inlines;

/// <summary>Шорткод в тексте.</summary>
internal sealed class ShortcodeInline : LeafInline
{
    public required MdShortcode Shortcode { get; init; }
}
