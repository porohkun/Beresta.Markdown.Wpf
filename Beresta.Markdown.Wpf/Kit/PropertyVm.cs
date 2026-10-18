namespace Beresta.Markdown.Wpf.Kit;

using ViewModels;

/// <summary>A row of the properties card: the key and the value as text.</summary>
public sealed record PropertyVm(IReadOnlyList<InlineVm> Key, IReadOnlyList<InlineVm> Value) : NodeVm;
