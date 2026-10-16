namespace MarkdigWpfViewer.ViewModels;

/// <summary>Заголовок уровня от 1 до 6; <paramref name="Id"/> — идентификатор от Markdig, явный (<c>{#id}</c>) или свой.</summary>
public sealed record HeadingVm(int Level, IReadOnlyList<InlineVm> Inlines, string? Id = null)
    : BlockVm;