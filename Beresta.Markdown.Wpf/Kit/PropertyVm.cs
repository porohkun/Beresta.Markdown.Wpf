namespace Beresta.Markdown.Wpf.Kit;

using ViewModels;

/// <summary>A row of the properties card: the key and the value as text.</summary>
public sealed record PropertyVm(IReadOnlyList<InlineVm> Key, IReadOnlyList<InlineVm> Value) : NodeVm
{
    /// <summary>The value as an input field shows it; a list — items through commas.</summary>
    public string ValueText { get; init; } = string.Empty;

    /// <summary>How the value is written in the source; a new value is written the same way.</summary>
    public MdValueKind ValueKind { get; init; }

    /// <summary>
    /// Where the value is in the source: from the colon after the key to the end of the value; a value
    /// that is only shown — <c>default</c>.
    /// </summary>
    public MdSourceSpan ValueSource { get; init; }

    /// <summary>Место и запись значения для правки; значение, которое только показывается, — <c>null</c>.</summary>
    internal FrontMatterValue? Place { get; init; }
}
