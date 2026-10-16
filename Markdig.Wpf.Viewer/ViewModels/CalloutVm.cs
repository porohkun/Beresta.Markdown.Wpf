namespace MarkdigWpfViewer.ViewModels;

/// <summary>Плашка: вид, заголовок и вложенные блоки.</summary>
/// <param name="Kind">Вид строчными буквами: note, tip, important, warning, caution или другой.</param>
/// <param name="Title">Свой заголовок; пусто — заголовок вида из подписей темы.</param>
public sealed record CalloutVm(string Kind, IReadOnlyList<InlineVm> Title, IReadOnlyList<BlockVm> Blocks)
    : BlockVm
{
    /// <summary>Плашку можно свернуть щелчком по заголовку.</summary>
    public bool IsFoldable { get; init; }

    /// <summary>Сворачиваемая плашка свёрнута, пока её не развернут.</summary>
    public bool IsFolded { get; init; }
}
