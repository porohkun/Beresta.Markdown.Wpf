namespace Beresta.Markdown.Wpf.ViewModels;

using Selection;

/// <summary>
/// Ссылка сноски: номер <c>[^метка]</c> в тексте, ведущий к определению, или <c>↩</c> в определении,
/// ведущий обратно к ссылке.
/// </summary>
/// <param name="Order">Номер сноски.</param>
/// <param name="Index">Номер ссылки на сноску в документе.</param>
/// <param name="IsBack">Обратная ссылка из определения.</param>
public sealed record FootnoteLinkVm(int Order, int Index, bool IsBack) : InlineVm, IMdCopyText
{
    /// <summary>Якорь, к которому ведёт ссылка.</summary>
    public string Target => IsBack ? $"fnref:{Index}" : $"fn:{Order}";

    public string CopyText => IsBack ? string.Empty : $"[{Order}]";
}