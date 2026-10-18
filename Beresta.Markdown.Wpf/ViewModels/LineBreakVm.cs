namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A line break; a hard one is marked in the source with a backslash or two spaces.</summary>
public sealed record LineBreakVm(bool IsHard = false) : InlineVm;