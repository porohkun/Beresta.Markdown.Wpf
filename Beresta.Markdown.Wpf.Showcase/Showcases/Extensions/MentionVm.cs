namespace Beresta.Markdown.Wpf.Showcase.Showcases.Extensions;

using Selection;
using ViewModels;

/// <summary>Упоминание участника; копируется как <c>@имя</c>.</summary>
public sealed record MentionVm(string Name) : InlineVm, IMdCopyText
{
    public string CopyText => "@" + Name;

    /// <summary>Профиль участника — куда ведёт щелчок по упоминанию.</summary>
    public string Url => "https://github.com/" + Name;
}