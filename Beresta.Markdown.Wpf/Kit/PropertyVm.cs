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

    /// <summary>The type of the value: the field of the application or the guess by the value.</summary>
    public MdPropertyType Type { get; init; } = MdPropertyType.Text;

    /// <summary>The items of a <see cref="MdPropertyType.List"/> value; other values — empty.</summary>
    public IReadOnlyList<string> Items { get; init; } = [];

    /// <summary>A <see cref="MdPropertyType.Bool"/> value is on: <c>true</c>, <c>yes</c> or <c>on</c>.</summary>
    public bool IsChecked { get; init; }

    /// <summary>The date of a <see cref="MdPropertyType.Date"/> value; not a date — <c>null</c>.</summary>
    public DateTime? Date { get; init; }

    /// <summary>Место и запись значения для правки; значение, которое только показывается, — <c>null</c>.</summary>
    internal FrontMatterValue? Place { get; init; }

    /// <summary>Ключ текстом, как в front matter без кавычек.</summary>
    internal string KeyText { get; init; } = string.Empty;
}
