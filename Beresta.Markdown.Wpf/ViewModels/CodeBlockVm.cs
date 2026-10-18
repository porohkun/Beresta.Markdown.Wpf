namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A code block; <paramref name="Syntax"/> is the language from the info string.</summary>
public sealed record CodeBlockVm(string? InfoString, string? Syntax, string Code)
    : BlockVm
{
    /// <summary>The code as one piece of text — for <c>MdTextBlock</c> and the text map.</summary>
    public IReadOnlyList<InlineVm> Runs { get; } = [new RunVm(Code)];

    /// <summary>The code is in a <c>```</c> fence; indented code has no fence.</summary>
    public bool IsFenced => Source.EndLine - Source.StartLine + 1 > Code.Split('\n').Length;

    /// <summary>Строка исходника, с которой начинается сам код, без ограждения.</summary>
    internal int CodeFirstLine => Source.StartLine + (IsFenced ? 1 : 0);
}