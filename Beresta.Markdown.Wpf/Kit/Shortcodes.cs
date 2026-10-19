namespace Beresta.Markdown.Wpf.Kit;

using System.IO;
using System.Windows;
using Extensions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Shortcodes <c>{%name arguments%}</c> and <c>{{&lt; name arguments &gt;}}</c> as a widget by the
/// name from <see cref="Handlers"/>; a shortcode without a handler stays text.
/// </summary>
public sealed class Shortcodes : MdExtension
{
    private readonly HashSet<Func<MdShortcode, NodeVm?>> _builtIn;

    /// <summary>Creates the extension with the built-in handlers.</summary>
    public Shortcodes()
    {
        Handlers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["youtube"] = shortcode => shortcode.Argument("id", 0) is { } id
                ? VideoSites.Card("https://www.youtube.com/watch?v=" + id, shortcode.Argument("title", 1) ?? "YouTube", $"https://img.youtube.com/vi/{id}/hqdefault.jpg", LoadThumbnails)
                : null,
            ["vimeo"] = shortcode => shortcode.Argument("id", 0) is { } id
                ? VideoSites.Card("https://vimeo.com/" + id, shortcode.Argument("title", 1) ?? "Vimeo", null, LoadThumbnails)
                : null,
            ["gist"] = shortcode => Gist(shortcode),
            ["pdf"] = shortcode => shortcode.Argument("src", 0) is { } url ? new LinkCardVm(url, Path.GetFileName(url.Split('?', '#')[0])) : null,
            ["toc"] = _ => new TocVm()
        };
        _builtIn = new(Handlers.Values, ReferenceEqualityComparer.Instance);
    }

    /// <summary>
    /// Handlers by the name of a shortcode: the model of an inline or block widget, <c>null</c> —
    /// the shortcode stays text. <c>youtube</c>, <c>vimeo</c>, <c>gist</c>, <c>pdf</c> and
    /// <c>toc</c> are there from the start.
    /// </summary>
    public Dictionary<string, Func<MdShortcode, NodeVm?>> Handlers { get; }

    /// <summary>Load video previews from the network; when off, a card has no preview.</summary>
    public bool LoadThumbnails { get; set; } = true;

    /// <inheritdoc/>
    public override bool AllowsBackgroundParse => Handlers.Values.All(handler => _builtIn.Contains(handler) || handler.Target is IMdBackgroundSafe);

    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<ShortcodeMarkdigExtension>();

    /// <inheritdoc/>
    public override IReadOnlyList<BlockVm>? ConvertBlocks(Block block, MdConvertContext context)
        => block is ParagraphBlock { Inline: { } inline } && Alone(inline) is { } shortcode && Handle(shortcode) is BlockVm widget ? [widget] : null;

    /// <inheritdoc/>
    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => inline is ShortcodeInline shortcode && Handle(shortcode.Shortcode) is InlineVm widget ? [widget] : null;

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        var key = item switch
        {
            VideoCardVm => "Md.VideoCard.Template",
            LinkCardVm => "Md.LinkCard.Template",
            TocVm => "Md.Toc.Template",
            _ => null
        };

        return key is not null && container is FrameworkElement element ? element.TryFindResource(key) as DataTemplate : null;
    }

    private static LinkCardVm? Gist(MdShortcode shortcode)
    {
        var path = shortcode.Arguments.Count >= 2 && !shortcode.Arguments[0].Contains('/')
            ? $"{shortcode.Arguments[0]}/{shortcode.Arguments[1]}"
            : shortcode.Argument("id", 0);
        return path is null ? null : new LinkCardVm("https://gist.github.com/" + path, "Gist " + path);
    }

    /// <summary>Шорткод, если он один занимает абзац.</summary>
    private static MdShortcode? Alone(ContainerInline inline)
    {
        var shortcodes = new List<ShortcodeInline>();
        for (var node = inline.FirstChild; node is not null; node = node.NextSibling)
        {
            switch (node)
            {
                case ShortcodeInline shortcode:
                    shortcodes.Add(shortcode);
                    break;
                case LiteralInline literal when literal.Content.IsEmptyOrWhitespace():
                case LineBreakInline:
                    break;
                default:
                    return null;
            }
        }

        return shortcodes is [var only] ? only.Shortcode : null;
    }

    private NodeVm? Handle(MdShortcode shortcode)
        => Handlers.TryGetValue(shortcode.Name, out var handler) ? handler(shortcode) : null;
}
