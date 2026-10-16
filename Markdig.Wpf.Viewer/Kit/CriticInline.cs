namespace MarkdigWpfViewer.Kit;

using Markdig.Syntax.Inlines;

/// <summary>Запись CriticMarkup: вставка, удаление, замена, выделение или комментарий.</summary>
internal sealed class CriticInline : LeafInline
{
    public required CriticKind Kind { get; init; }

    /// <summary>Текст записи; у замены — прежний текст.</summary>
    public required string Text { get; init; }

    /// <summary>Новый текст замены; у других видов — <c>null</c>.</summary>
    public string? Replacement { get; init; }
}
