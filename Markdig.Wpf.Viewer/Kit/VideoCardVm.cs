namespace MarkdigWpfViewer.Kit;

using Selection;
using ViewModels;

/// <summary>Карточка видео: превью, название и значок воспроизведения; щелчок идёт путём ссылок.</summary>
/// <param name="Href">Адрес видео.</param>
/// <param name="Title">Название — из alt картинки.</param>
/// <param name="Preview">Превью через загрузчик картинок вьюера; нет — карточка без картинки.</param>
public sealed record VideoCardVm(string Href, string Title, ImageVm? Preview) : InlineVm, IMdCopyText
{
    public string CopyText => Title;
}
