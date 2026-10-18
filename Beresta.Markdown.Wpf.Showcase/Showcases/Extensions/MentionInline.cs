namespace Beresta.Markdown.Wpf.Showcase.Showcases.Extensions;

using Markdig.Syntax.Inlines;

/// <summary>Узел Markdig для <c>@имя</c>.</summary>
public sealed class MentionInline : LeafInline
{
    public required string Name { get; init; }
}