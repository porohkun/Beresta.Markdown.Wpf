namespace MarkdigWpfViewer.Showcase.Extensions;

using Selection;
using ViewModels;

/// <summary>Упоминание участника; копируется как <c>@имя</c>.</summary>
public sealed record MentionVm(string Name) : InlineVm, IMdCopyText
{
    public string CopyText => "@" + Name;
}