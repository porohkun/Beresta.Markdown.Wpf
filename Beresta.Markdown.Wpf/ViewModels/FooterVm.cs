namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A footer: lines <c>^^ text</c>, in small text.</summary>
public sealed record FooterVm(IReadOnlyList<BlockVm> Blocks)
    : BlockVm;
