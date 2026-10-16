namespace MarkdigWpfViewer;

using System.Windows.Media;

/// <summary>Откуда вьюер берёт картинки; ставится в <see cref="MarkdownViewer.ImageLoader"/>.</summary>
public interface IMdImageLoader
{
    /// <summary>
    /// Картинка; <paramref name="url"/> — адрес ровно из разметки, <paramref name="uri"/> — он же,
    /// разобранный от <see cref="MarkdownViewer.BaseUri"/>. Картинки нет — <c>null</c>.
    /// </summary>
    Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken);
}