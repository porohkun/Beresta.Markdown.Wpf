namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>
/// Horizontal alignment of blocks — from the HTML attribute <c>align</c> or the tag
/// <c>&lt;center&gt;</c>.
/// </summary>
public enum BlockAlignment
{
    /// <summary>To the left edge.</summary>
    Left,

    /// <summary>Centered.</summary>
    Center,

    /// <summary>To the right edge.</summary>
    Right,

    /// <summary>Justified to both edges.</summary>
    Justify
}
