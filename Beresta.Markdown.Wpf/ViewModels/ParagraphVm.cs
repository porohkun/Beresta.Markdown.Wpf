namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A paragraph.</summary>
public sealed record ParagraphVm(IReadOnlyList<InlineVm> Inlines)
    : BlockVm;