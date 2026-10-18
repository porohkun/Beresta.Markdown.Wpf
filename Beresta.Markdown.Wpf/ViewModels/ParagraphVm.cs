namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>Абзац.</summary>
public sealed record ParagraphVm(IReadOnlyList<InlineVm> Inlines)
    : BlockVm;