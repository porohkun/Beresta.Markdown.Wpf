namespace Beresta.Markdown.Wpf.Kit;

using Selection;
using ViewModels;

/// <summary>A link card: a title and an address; a click goes the way of links.</summary>
/// <param name="Href">The address.</param>
/// <param name="Title">The title.</param>
public sealed record LinkCardVm(string Href, string Title) : InlineVm, IMdCopyText
{
    /// <inheritdoc/>
    public string CopyText => Title;
}
