namespace Beresta.Markdown.Wpf.Kit;

using Selection;
using ViewModels;

/// <summary>Комментарий CriticMarkup — значок с подсказкой; в скопированный текст не попадает.</summary>
public sealed record CriticCommentVm(string Text) : InlineVm, IMdCopyText
{
    public string CopyText => string.Empty;
}
