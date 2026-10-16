namespace MarkdigWpfViewer.ViewModels;

/// <summary>Подвал: строки <c>^^ текст</c>, мелким текстом.</summary>
public sealed record FooterVm(IReadOnlyList<BlockVm> Blocks)
    : BlockVm;
