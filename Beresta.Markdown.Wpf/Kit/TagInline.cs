namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Syntax.Inlines;

/// <summary>Тег <c>#тег</c> или <c>#тег/вложенный</c>.</summary>
internal sealed class TagInline : LeafInline
{
    /// <summary>Имя без решётки.</summary>
    public required string Name { get; init; }
}
