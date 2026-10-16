namespace MarkdigWpfViewer.Kit;

using Markdig.Parsers;
using Markdig.Syntax;

/// <summary>Предупреждение MkDocs <c>!!! вид "Заголовок"</c> с блоками под отступом.</summary>
internal sealed class AdmonitionBlock(BlockParser parser) : ContainerBlock(parser)
{
    public string Kind { get; init; } = "note";

    /// <summary>Свой заголовок из кавычек; нет — <c>null</c>.</summary>
    public string? Title { get; init; }

    /// <summary>Сворачиваемое: <c>???</c> или <c>???+</c>.</summary>
    public bool IsCollapsible { get; init; }

    /// <summary>Сворачиваемое развёрнуто сразу: <c>???+</c>.</summary>
    public bool IsExpanded { get; init; }
}
