namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>Сворачиваемый блок: заголовок и вложенные блоки.</summary>
/// <param name="Summary">Заголовок; пусто — заголовок из подписей темы.</param>
public sealed record DetailsVm(IReadOnlyList<InlineVm> Summary, IReadOnlyList<BlockVm> Blocks)
    : BlockVm
{
    /// <summary>Развёрнут, пока его не свернут.</summary>
    public bool IsOpen { get; init; }
}
