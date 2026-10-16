namespace MarkdigWpfViewer.Kit;

using Markdig.Syntax.Inlines;

/// <summary>Спойлер <c>||текст||</c>.</summary>
internal sealed class SpoilerInline : LeafInline
{
    public required string Text { get; init; }
}
