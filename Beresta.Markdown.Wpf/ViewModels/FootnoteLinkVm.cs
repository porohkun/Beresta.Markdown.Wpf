namespace Beresta.Markdown.Wpf.ViewModels;

using Selection;

/// <summary>
/// A footnote link: the number <c>[^label]</c> in the text that leads to the definition, or
/// <c>↩</c> in the definition that leads back to the link.
/// </summary>
/// <param name="Order">The number of the footnote.</param>
/// <param name="Index">The number of the link to the footnote in the document.</param>
/// <param name="IsBack">A back link from the definition.</param>
public sealed record FootnoteLinkVm(int Order, int Index, bool IsBack) : InlineVm, IMdCopyText
{
    /// <summary>The anchor the link leads to.</summary>
    public string Target => IsBack ? $"fnref:{Index}" : $"fn:{Order}";

    /// <inheritdoc/>
    public string CopyText => IsBack ? string.Empty : $"[{Order}]";
}