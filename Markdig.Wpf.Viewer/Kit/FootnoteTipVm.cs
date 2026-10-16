namespace MarkdigWpfViewer.Kit;

using Selection;
using ViewModels;

/// <summary>Ссылка на сноску с её текстом в подсказке.</summary>
/// <param name="Order">Номер сноски.</param>
/// <param name="Markdown">Текст сноски — markdown её блоков.</param>
public sealed record FootnoteTipVm(int Order, string Markdown) : InlineVm, IMdCopyText
{
    public string CopyText => $"[{Order}]";
}
