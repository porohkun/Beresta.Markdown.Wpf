namespace MarkdigWpfViewer.ViewModels;

/// <summary>Блок кода; <paramref name="Syntax"/> — язык из строки информации.</summary>
public sealed record CodeBlockVm(string? InfoString, string? Syntax, string Code)
    : BlockVm
{
    /// <summary>Код одним куском текста — для <c>MdTextBlock</c> и текстовой карты.</summary>
    public IReadOnlyList<InlineVm> Runs { get; } = [new RunVm(Code)];

    /// <summary>Строка исходника, с которой начинается сам код, без ограждения.</summary>
    internal int CodeFirstLine
        => Source.StartLine + (Source.EndLine - Source.StartLine + 1 > Code.Split('\n').Length ? 1 : 0);
}
