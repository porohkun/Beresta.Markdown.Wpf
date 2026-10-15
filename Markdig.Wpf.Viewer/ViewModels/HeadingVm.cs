namespace MarkdigWpfViewer.ViewModels;

/// <summary>Заголовок уровня от 1 до 6.</summary>
public sealed record HeadingVm(int Level, IReadOnlyList<InlineVm> Inlines)
    : BlockVm;
