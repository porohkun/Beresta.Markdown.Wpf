namespace MarkdigWpfViewer.ViewModels;

using Abstractions;

/// <param name="Hyperlink">Разбор <paramref name="Href"/>; ссылки нет — <c>null</c>.</param>
/// <param name="Href">
/// Цель ссылки как она записана в markdown. Хранится рядом с <paramref name="Hyperlink"/>, а не
/// выводится из него: адрес с незаконной по RFC 3986 схемой (<c>codex_page:...</c>) разбирается
/// в относительный <see cref="Uri"/>, и обработчику своих ссылок нужна исходная строка.
/// </param>
public sealed record InlineVm(
    string Text,
    bool Bold = false,
    bool Italic = false,
    bool Strike = false,
    bool Code = false,
    Uri? Hyperlink = null,
    bool LineBreak = false,
    string? Href = null)
    : IMdBlockVm;
