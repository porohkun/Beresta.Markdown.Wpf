namespace MarkdigWpfViewer.Kit;

using Selection;
using ViewModels;

/// <summary>Кнопки «принять» и «отклонить» у правки CriticMarkup; ни в текст, ни в разметку не попадают.</summary>
/// <param name="Change">Место правки в исходнике вместе с её разметкой.</param>
/// <param name="Accepted">Текст на месте правки, если её принять.</param>
/// <param name="Rejected">Текст на месте правки, если её отклонить.</param>
public sealed record CriticDecisionVm(MdSourceSpan Change, string Accepted, string Rejected) : InlineVm, IMdCopyText, IMdMarkdownText
{
    public string CopyText => string.Empty;

    public string MarkdownText => string.Empty;
}
