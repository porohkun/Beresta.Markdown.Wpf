namespace Beresta.Markdown.Wpf.Selection;

/// <summary>A heading of the document for a table of contents.</summary>
/// <param name="Level">The level from 1 to 6.</param>
/// <param name="Text">The text of the heading without markup.</param>
/// <param name="Id">The anchor, as on GitHub: the link <c>#id</c> leads to it.</param>
/// <param name="Range">The piece of the text map taken by the heading.</param>
/// <param name="SourceLine">The source line, counting from zero.</param>
public sealed record MdHeading(int Level, string Text, string Id, MdTextRange Range, int SourceLine)
{
    /// <summary>The link to the heading: <c>#id</c>.</summary>
    public string Href => "#" + Id;
}