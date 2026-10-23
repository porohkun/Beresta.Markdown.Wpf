namespace Beresta.Markdown.Wpf.Kit;

/// <summary>How a value of front matter is written; a new value is written the same way.</summary>
public enum MdValueKind
{
    /// <summary>Nested, multiline or otherwise complex: only shown, not edited.</summary>
    ReadOnly,

    /// <summary>A plain value: <c>key: value</c>.</summary>
    Plain,

    /// <summary>In single quotes: <c>key: 'value'</c>.</summary>
    SingleQuoted,

    /// <summary>In double quotes: <c>key: "value"</c>.</summary>
    DoubleQuoted,

    /// <summary>A list in brackets: <c>key: [a, b]</c>.</summary>
    FlowList,

    /// <summary>A list of lines: <c>- a</c> under the key.</summary>
    BlockList
}
