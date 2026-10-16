namespace MarkdigWpfViewer.ViewModels;

/// <summary>Кусок текста с одним набором начертаний.</summary>
/// <param name="Hyperlink">Разбор <paramref name="Href"/>; ссылки нет — <c>null</c>.</param>
/// <param name="Href">Цель ссылки; ссылки нет — <c>null</c>.</param>
/// <param name="LinkTitle">Заголовок ссылки (<c>[текст](адрес "заголовок")</c>); нет — <c>null</c>.</param>
/// <param name="Kbd">Клавиша, <c>&lt;kbd&gt;</c>.</param>
/// <param name="Subscript">Нижний индекс, <c>~текст~</c> или <c>&lt;sub&gt;</c>.</param>
/// <param name="Superscript">Верхний индекс, <c>^текст^</c> или <c>&lt;sup&gt;</c>.</param>
/// <param name="Marked">Выделение маркером, <c>==текст==</c> или <c>&lt;mark&gt;</c>.</param>
/// <param name="Inserted">Вставка, <c>++текст++</c>, <c>&lt;ins&gt;</c> или <c>&lt;u&gt;</c>.</param>
/// <param name="Cite">Название произведения, <c>""текст""</c> или <c>&lt;cite&gt;</c>.</param>
/// <param name="Abbreviation">Расшифровка сокращения из <c>*[метка]: расшифровка</c>; нет — <c>null</c>.</param>
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
    bool Superscript = false,
    bool Marked = false,
    bool Inserted = false,
    bool Cite = false,
    string? Abbreviation = null)
    : InlineVm
{
    /// <summary>Имя оформления: кусок рисуется стилем <c>Span</c> с ключом темы <c>Md.&lt;имя&gt;.Style</c>; нет — <c>null</c>.</summary>
    public string? StyleName { get; init; }

    /// <summary>Обрамление куска в разметке выделенного; нет — кусок пишется без обрамления.</summary>
    public MarkupFrame? Markup { get; init; }
}