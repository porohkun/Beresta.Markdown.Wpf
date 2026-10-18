namespace Beresta.Markdown.Wpf.Kit;

using System.Windows;
using Extensions;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Video cards: an image link to YouTube, Vimeo, RuTube and an image with a video address become a
/// card — a preview, a title from the alt text, a play icon. The look is the theme template
/// <c>Md.VideoCard.Template</c>.
/// </summary>
public sealed class VideoCards : MdExtension
{
    /// <summary>Load previews from the network; when off, a card has no preview unless it is local.</summary>
    public bool LoadThumbnails { get; set; } = true;

    /// <inheritdoc/>
    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
    {
        switch (inline)
        {
            case LinkInline { IsImage: false, FirstChild: LinkInline { IsImage: true, NextSibling: null } image } link when VideoSites.IsVideo(link.Url):
                return VideoSites.Card(link.Url!, Alt(image, context), image.Url, LoadThumbnails);

            case LinkInline { IsImage: true } image when VideoSites.IsVideo(image.Url):
                return VideoSites.Card(image.Url!, Alt(image, context), VideoSites.ThumbnailOf(image.Url!), LoadThumbnails);

            default:
                return null;
        }
    }

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is VideoCardVm && container is FrameworkElement element ? element.TryFindResource("Md.VideoCard.Template") as DataTemplate : null;

    private static string Alt(LinkInline image, MdConvertContext context)
        => string.Concat(context.ConvertInlines(image).OfType<RunVm>().Select(run => run.Text));
}
