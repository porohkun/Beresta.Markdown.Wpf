namespace MarkdigWpfViewer.Kit;

using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Встраивание картинок <c>![[файл.png]]</c>, <c>![[файл.png|200]]</c>, <c>![[файл.png|200x100]]</c> —
/// картинкой через загрузчик картинок вьюера. Встраивание не картинки — вики-ссылкой на него.
/// </summary>
public sealed partial class WikiEmbeds : MdExtension
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tif", ".tiff", ".ico", ".svg"
    };

    /// <summary>Знает, какие страницы есть, — для встраивания не картинки.</summary>
    public IMdLinkResolver? Resolver { get; set; }

    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<WikiMarkdigExtension>();

    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
    {
        if (inline is not WikiLinkInline { IsEmbed: true } embed)
        {
            return null;
        }

        if (!ImageExtensions.Contains(Path.GetExtension(embed.Target)))
        {
            return [WikiLinks.LinkRun(embed, context, Resolver)];
        }

        var name = Path.GetFileName(embed.Target);
        if (Size().Match(embed.Label ?? string.Empty) is not { Success: true } size)
        {
            return [new ImageVm(embed.Target, embed.Label is { Length: > 0 } alt ? alt : name)];
        }

        return
        [
            new ImageVm(embed.Target, name)
            {
                Width = double.Parse(size.Groups["width"].Value, CultureInfo.InvariantCulture),
                Height = size.Groups["height"].Success ? double.Parse(size.Groups["height"].Value, CultureInfo.InvariantCulture) : null
            }
        ];
    }

    [GeneratedRegex(@"^(?<width>\d+)(?:x(?<height>\d+))?$")]
    private static partial Regex Size();
}
