namespace Beresta.Markdown.Wpf.Kit;

using Selection;
using ViewModels;

/// <summary>A video card: a preview, a title and a play icon; a click goes the way of links.</summary>
/// <param name="Href">The address of the video.</param>
/// <param name="Title">The title, from the alt text of the image.</param>
/// <param name="Preview">The preview through the image loader of the viewer; none — a card without an image.</param>
public sealed record VideoCardVm(string Href, string Title, ImageVm? Preview) : InlineVm, IMdCopyText
{
    /// <inheritdoc/>
    public string CopyText => Title;
}
