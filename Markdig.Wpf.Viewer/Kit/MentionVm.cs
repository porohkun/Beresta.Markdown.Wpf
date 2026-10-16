namespace MarkdigWpfViewer.Kit;

using Selection;
using ViewModels;

/// <summary>Упоминание участника чипом; копируется как <c>@</c> и отображаемое имя.</summary>
/// <param name="Name">Имя из разметки.</param>
/// <param name="DisplayName">Имя для показа — от резолвера приложения или как написано.</param>
public sealed record MentionVm(string Name, string DisplayName) : InlineVm, IMdCopyText
{
    /// <summary>Цель ссылки упоминания: <c>mention:имя</c>.</summary>
    public string Href => "mention:" + Name;

    public string CopyText => "@" + DisplayName;
}
