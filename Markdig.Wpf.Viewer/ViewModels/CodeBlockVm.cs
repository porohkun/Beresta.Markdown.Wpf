namespace MarkdigWpfViewer.ViewModels;

using Abstractions;

public sealed record CodeBlockVm(string? InfoString, string? Syntax, string Code)
    : IMdBlockVm;