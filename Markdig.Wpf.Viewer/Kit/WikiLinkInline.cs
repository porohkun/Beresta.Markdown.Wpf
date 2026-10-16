namespace MarkdigWpfViewer.Kit;

using Markdig.Syntax.Inlines;

/// <summary>Вики-ссылка <c>[[Страница#Раздел|подпись]]</c> или встраивание <c>![[файл]]</c>.</summary>
internal sealed class WikiLinkInline : LeafInline
{
    /// <summary>Страница или файл; пусто — раздел этой же страницы.</summary>
    public required string Target { get; init; }

    public string? Section { get; init; }

    /// <summary>Подпись после <c>|</c>; у встраивания — подпись или размер.</summary>
    public string? Label { get; init; }

    /// <summary>Встраивание: <c>![[…]]</c>.</summary>
    public bool IsEmbed { get; init; }
}
