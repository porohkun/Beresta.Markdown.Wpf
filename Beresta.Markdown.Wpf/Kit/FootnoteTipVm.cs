namespace Beresta.Markdown.Wpf.Kit;

using Selection;
using ViewModels;

/// <summary>A link to a footnote with its text in a tooltip.</summary>
/// <param name="Order">The number of the footnote.</param>
/// <param name="Markdown">The text of the footnote: the Markdown of its blocks.</param>
public sealed record FootnoteTipVm(int Order, string Markdown) : InlineVm, IMdCopyText
{
    /// <inheritdoc/>
    public string CopyText => $"[{Order}]";
}
