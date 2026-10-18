namespace Beresta.Markdown.Wpf.Selection;

/// <summary>How a mark is drawn.</summary>
public enum MdMarkKind
{
    /// <summary>A fill under the text.</summary>
    Fill,

    /// <summary>A line under the text.</summary>
    Underline,

    /// <summary>A frame around the text, its own for every screen line.</summary>
    Frame,

    /// <summary>Rows across the whole width of the document.</summary>
    Row,

    /// <summary>A bar in the margin by the line numbers.</summary>
    Bar,

    /// <summary>An icon in the margin by the number of the first line.</summary>
    Icon,

    /// <summary>A vertical line at the start of the piece; for an empty piece — at its place.</summary>
    Caret
}
