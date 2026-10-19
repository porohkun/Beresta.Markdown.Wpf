namespace Beresta.Markdown.Wpf;

using System.Windows.Media;

/// <summary>Where the viewer takes images from; set in <see cref="MarkdownViewer.ImageLoader"/>.</summary>
public interface IMdImageLoader
{
    /// <summary>
    /// An image; <paramref name="url"/> is the address exactly as in the markup, <paramref name="uri"/>
    /// is the same address resolved against <see cref="MarkdownViewer.BaseUri"/>,
    /// <paramref name="decodeWidth"/> is the width in pixels to decode the image to at most, 0 — full
    /// size. No image — <c>null</c>.
    /// </summary>
    Task<ImageSource?> LoadAsync(string url, Uri? uri, int decodeWidth, CancellationToken cancellationToken);
}
