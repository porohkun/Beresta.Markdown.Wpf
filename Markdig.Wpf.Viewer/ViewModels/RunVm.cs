namespace MarkdigWpfViewer.ViewModels;

/// <summary>Кусок текста с одним набором начертаний.</summary>
/// <param name="Hyperlink">Разбор <paramref name="Href"/>; ссылки нет — <c>null</c>.</param>
/// <param name="Href">Цель ссылки; ссылки нет — <c>null</c>.</param>
public sealed record RunVm(
    string Text,
    bool Bold = false,
    bool Italic = false,
    bool Strike = false,
    bool Code = false,
    Uri? Hyperlink = null,
    string? Href = null)
    : InlineVm;
