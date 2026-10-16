namespace MarkdigWpfViewer.ViewModels;

/// <summary>Кусок текста с одним набором начертаний.</summary>
/// <param name="Hyperlink">Разбор <paramref name="Href"/>; ссылки нет — <c>null</c>.</param>
/// <param name="Href">Цель ссылки; ссылки нет — <c>null</c>.</param>
/// <param name="LinkTitle">Заголовок ссылки (<c>[текст](адрес "заголовок")</c>); нет — <c>null</c>.</param>
/// <param name="Kbd">Клавиша, <c>&lt;kbd&gt;</c>.</param>
/// <param name="Subscript">Нижний индекс, <c>&lt;sub&gt;</c>.</param>
/// <param name="Superscript">Верхний индекс, <c>&lt;sup&gt;</c>.</param>
public sealed record RunVm(
    string Text,
    bool Bold = false,
    bool Italic = false,
    bool Strike = false,
    bool Code = false,
    Uri? Hyperlink = null,
    string? Href = null,
    string? LinkTitle = null,
    bool Kbd = false,
    bool Subscript = false,
    bool Superscript = false)
    : InlineVm;
