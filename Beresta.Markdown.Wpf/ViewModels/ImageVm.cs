namespace Beresta.Markdown.Wpf.ViewModels;

using Selection;

/// <summary>
/// An image; <paramref name="Url"/> is the address exactly as in the markup, <paramref name="Alt"/>
/// is its text.
/// </summary>
public sealed record ImageVm(string Url, string Alt) : InlineVm, IMdCopyText
{
    /// <summary>The title of the image — a tooltip; none — <c>null</c>.</summary>
    public string? Title { get; init; }

    /// <summary>The width from the attributes, in pixels; not set — <c>null</c>.</summary>
    public double? Width { get; init; }

    /// <summary>The height from the attributes, in pixels; not set — <c>null</c>.</summary>
    public double? Height { get; init; }

    /// <summary>The target of the link around the image; no link — <c>null</c>.</summary>
    public string? Href { get; init; }

    /// <summary>The parsed <see cref="Href"/>.</summary>
    public Uri? Hyperlink { get; init; }

    /// <summary>The title of the link around the image; none — <c>null</c>.</summary>
    public string? LinkTitle { get; init; }

    /// <inheritdoc/>
    public string CopyText => Alt;
}