namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>Подпись фигуры — текст после <c>^^^</c> на открывающей или закрывающей строке.</summary>
public sealed record FigureCaptionVm(IReadOnlyList<InlineVm> Inlines)
    : BlockVm;
