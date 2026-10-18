namespace Beresta.Markdown.Wpf.Selection;

/// <summary>
/// An inline widget that writes its own text to the markup of the selection instead of the source
/// of the node.
/// </summary>
public interface IMdMarkdownText
{
    /// <summary>The text for the markup of the selection; empty — the widget does not get into it.</summary>
    string MarkdownText { get; }
}
