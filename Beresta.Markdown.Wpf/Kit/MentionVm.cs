namespace Beresta.Markdown.Wpf.Kit;

using Selection;
using ViewModels;

/// <summary>A mention of a person as a chip; copied as <c>@</c> and the display name.</summary>
/// <param name="Name">The name from the markup.</param>
/// <param name="DisplayName">The name to show: from the resolver of the application or as written.</param>
public sealed record MentionVm(string Name, string DisplayName) : InlineVm, IMdCopyText
{
    /// <summary>The target of the mention link: <c>mention:name</c>.</summary>
    public string Href => "mention:" + Name;

    /// <inheritdoc/>
    public string CopyText => "@" + DisplayName;
}
