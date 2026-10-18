namespace Beresta.Markdown.Wpf.Kit;

using Selection;
using ViewModels;

/// <summary>Карточка-ссылка: название и адрес; щелчок идёт путём ссылок.</summary>
/// <param name="Href">Адрес.</param>
/// <param name="Title">Название.</param>
public sealed record LinkCardVm(string Href, string Title) : InlineVm, IMdCopyText
{
    public string CopyText => Title;
}
