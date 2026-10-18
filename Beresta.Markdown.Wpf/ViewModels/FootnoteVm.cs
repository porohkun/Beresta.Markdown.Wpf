namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>Определение сноски <c>[^метка]: текст</c>; <paramref name="Order"/> — её номер в документе.</summary>
public sealed record FootnoteVm(int Order, string Label, IReadOnlyList<BlockVm> Blocks)
    : BlockVm;