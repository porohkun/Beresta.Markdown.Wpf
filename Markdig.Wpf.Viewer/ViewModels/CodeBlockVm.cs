namespace MarkdigWpfViewer.ViewModels;

/// <summary>Блок кода; <paramref name="Syntax"/> — язык из строки информации.</summary>
public sealed record CodeBlockVm(string? InfoString, string? Syntax, string Code)
    : BlockVm;
