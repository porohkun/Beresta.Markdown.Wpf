namespace Beresta.Markdown.Wpf.Kit;

using System.Windows;
using Extensions;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Карточки видео: картинка-ссылка на YouTube, Vimeo, RuTube и картинка с адресом видео становятся
/// карточкой — превью, название из alt, значок воспроизведения. Вид — шаблон темы <c>Md.VideoCard.Template</c>.
/// </summary>
public sealed class VideoCards : MdExtension
{
    /// <summary>Грузить превью из сети; выключено — карточка без превью, если оно не локальное.</summary>
    public bool LoadThumbnails { get; set; } = true;

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

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is VideoCardVm && container is FrameworkElement element ? element.TryFindResource("Md.VideoCard.Template") as DataTemplate : null;

    private static string Alt(LinkInline image, MdConvertContext context)
        => string.Concat(context.ConvertInlines(image).OfType<RunVm>().Select(run => run.Text));
}
