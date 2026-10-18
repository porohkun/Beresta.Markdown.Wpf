namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>Блоки с общим выравниванием: содержимое <c>&lt;div align&gt;</c>, <c>&lt;p align&gt;</c>, <c>&lt;center&gt;</c>.</summary>
public sealed record AlignedVm(BlockAlignment Alignment, IReadOnlyList<BlockVm> Blocks) : BlockVm;
