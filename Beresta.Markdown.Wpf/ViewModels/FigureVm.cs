namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A <c>^^^</c> figure: the content and the captions <see cref="FigureCaptionVm"/> at their places.</summary>
public sealed record FigureVm(IReadOnlyList<BlockVm> Blocks)
    : BlockVm;
