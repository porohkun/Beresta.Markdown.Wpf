namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A code block; <paramref name="Syntax"/> is the language from the info string.</summary>
public sealed record CodeBlockVm(string? InfoString, string? Syntax, string Code)
    : BlockVm
{
    /// <summary>The code as one piece of text — for <c>MdTextBlock</c> and the text map.</summary>
    public IReadOnlyList<InlineVm> Runs { get; private init; } = [new RunVm(Code)];

    /// <summary>The code; a new one also gets a new <see cref="Runs"/>.</summary>
    public string Code
    {
        get;
        init
        {
            field = value;
            Runs = [new RunVm(value)];
        }
    } = Code;

    /// <summary>
    /// The code is in a <c>```</c> fence; indented code has no fence. Parsing sets it; an extension
    /// that creates a code block sets it itself.
    /// </summary>
    public bool IsFenced { get; init; }

    /// <summary>Строка исходника, с которой начинается сам код, без ограждения.</summary>
    internal int CodeFirstLine => Source.StartLine + (IsFenced ? 1 : 0);
}
