namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using Selection;
using ViewModels;

/// <summary>
/// Wiki links <c>[[Page]]</c>, <c>[[Page#Section]]</c>, <c>[[Page|caption]]</c> as a link
/// <c>wiki:Page#Section</c> with the style <c>WikiLink</c>; <c>[[#Section]]</c> leads to a section
/// of this document. A page the <see cref="Resolver"/> does not know gets the style
/// <c>WikiLink.Missing</c>.
/// </summary>
public sealed class WikiLinks : MdExtension
{
    /// <summary>Knows which pages exist; none — every page exists.</summary>
    public IMdLinkResolver? Resolver { get; set; }

    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<WikiMarkdigExtension>();

    /// <inheritdoc/>
    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => inline is WikiLinkInline { IsEmbed: false } link ? [LinkRun(link, context, Resolver)] : null;

    /// <summary>Цель ссылки: <c>wiki:Страница#Раздел</c>, у раздела этого документа — его якорь.</summary>
    internal static string Href(WikiLinkInline link)
        => link.Target.Length == 0
            ? "#" + MdTextMap.AnchorOf(link.Section ?? string.Empty)
            : "wiki:" + link.Target + (link.Section is { Length: > 0 } section ? "#" + section : string.Empty);

    /// <summary>Текст ссылки: подпись, страница или «Страница › Раздел».</summary>
    internal static string Caption(WikiLinkInline link)
        => link.Label is { Length: > 0 } label ? label
            : link.Section is not { Length: > 0 } section ? link.Target
            : link.Target.Length == 0 ? section
            : $"{link.Target} › {section}";

    /// <summary>Ссылка куском текста; в разметку выделенного — снова вики-ссылкой.</summary>
    internal static RunVm LinkRun(WikiLinkInline link, MdConvertContext context, IMdLinkResolver? resolver)
    {
        var href = Href(link);
        var caption = Caption(link);
        var written = link.Target + (link.Section is null ? string.Empty : "#" + link.Section);
        var exists = link.Target.Length == 0 || (resolver?.PageExists(link.Target) ?? true);

        return context.CreateRun(caption) with
        {
            Href = href,
            Hyperlink = KitLinks.UriOf(href),
            StyleName = exists ? "WikiLink" : "WikiLink.Missing",
            Markup = new(caption == written ? "[[" : $"[[{written}|", "]]") { ReplacesLink = true }
        };
    }
}
