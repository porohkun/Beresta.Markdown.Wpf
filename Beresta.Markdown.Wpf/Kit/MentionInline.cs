namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Syntax.Inlines;

/// <summary>Упоминание <c>@имя</c>.</summary>
internal sealed class MentionInline : LeafInline
{
    /// <summary>Имя без <c>@</c>.</summary>
    public required string Name { get; init; }
}
