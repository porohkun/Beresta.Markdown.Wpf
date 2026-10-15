namespace MarkdigWpfViewer.Showcase.Extensions;

using ViewModels;

/// <summary>Заметка: заголовок и вложенные блоки.</summary>
public sealed record NoteVm(string Title, IReadOnlyList<BlockVm> Blocks) : BlockVm;
