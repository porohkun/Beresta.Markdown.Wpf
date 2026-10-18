namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>
/// A footnote definition <c>[^label]: text</c>; <paramref name="Order"/> is its number in the
/// document.
/// </summary>
public sealed record FootnoteVm(int Order, string Label, IReadOnlyList<BlockVm> Blocks)
    : BlockVm;