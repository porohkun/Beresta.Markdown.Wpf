namespace Beresta.Markdown.Wpf.Selection;

/// <summary>
/// An indivisible widget with text for copying: in the text map it is one placeholder character,
/// and the copied text gets <see cref="CopyText"/>.
/// </summary>
public interface IMdCopyText
{
    /// <summary>The text of the widget in the copied text.</summary>
    string CopyText { get; }
}