namespace MarkdigWpfViewer.Kit;

using ViewModels;

/// <summary>Строка карточки свойств: ключ и значение текстом.</summary>
public sealed record PropertyVm(IReadOnlyList<InlineVm> Key, IReadOnlyList<InlineVm> Value) : NodeVm;
