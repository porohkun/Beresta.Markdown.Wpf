namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A caption of a figure — the text after <c>^^^</c> on the opening or the closing line.</summary>
public sealed record FigureCaptionVm(IReadOnlyList<InlineVm> Inlines)
    : BlockVm;
