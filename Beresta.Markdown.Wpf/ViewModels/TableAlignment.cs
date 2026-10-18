namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>The alignment of a table column from the delimiter row: <c>:---</c>, <c>:---:</c>, <c>---:</c>.</summary>
public enum TableAlignment
{
    /// <summary>Not set — as the text around.</summary>
    Default,

    /// <summary>To the left edge, <c>:---</c>.</summary>
    Left,

    /// <summary>Centered, <c>:---:</c>.</summary>
    Center,

    /// <summary>To the right edge, <c>---:</c>.</summary>
    Right
}