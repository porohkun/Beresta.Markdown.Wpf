namespace MarkdigWpfViewer.Showcase.Showcases.Copying;

/// <summary>Пункт выбора: что кладётся в буфер и ключ его подписи в строках витрины.</summary>
public sealed record CopyChoice(CopyKind Kind, string TitleKey);
