namespace Beresta.Markdown.Wpf;

using System.Windows.Media;

/// <summary>Where the viewer takes images from; set in <see cref="MarkdownViewer.ImageLoader"/>.</summary>
public interface IMdImageLoader
{
    /// <summary>
    /// An image; <paramref name="url"/> is the address exactly as in the markup,
    /// <paramref name="uri"/> is the same address resolved against
    /// <see cref="MarkdownViewer.BaseUri"/>. No image — <c>null</c>.
    /// </summary>
    Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken);
}