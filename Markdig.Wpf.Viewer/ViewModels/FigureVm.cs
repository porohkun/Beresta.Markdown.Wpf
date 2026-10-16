namespace MarkdigWpfViewer.ViewModels;

/// <summary>Фигура <c>^^^</c>: содержимое и подписи <see cref="FigureCaptionVm"/> на своих местах.</summary>
public sealed record FigureVm(IReadOnlyList<BlockVm> Blocks)
    : BlockVm;
