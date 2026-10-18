namespace Beresta.Markdown.Wpf.Kit;

using Selection;
using ViewModels;

/// <summary>A CriticMarkup comment: an icon with a tooltip; not included in copied text.</summary>
public sealed record CriticCommentVm(string Text) : InlineVm, IMdCopyText
{
    /// <inheritdoc/>
    public string CopyText => string.Empty;
}
