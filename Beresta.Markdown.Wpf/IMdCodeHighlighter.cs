namespace Beresta.Markdown.Wpf;

/// <summary>
/// Highlights code blocks: gives the pieces of the code their looks by the language. A look named
/// <c>X</c> is drawn with the theme style <c>Md.Code.X.Style</c>.
/// </summary>
public interface IMdCodeHighlighter
{
    /// <summary>The pieces of the code with their looks; the language is unknown — <c>null</c>.</summary>
    IReadOnlyList<MdCodeSpan>? Highlight(string language, string code);
}
