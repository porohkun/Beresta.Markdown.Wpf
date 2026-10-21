namespace Beresta.Markdown.Wpf.Extensions;

/// <summary>
/// How the source of a document looks in an editor: named looks of pieces of the source text and of
/// source lines. An editor takes the fonts and colors of a look named <c>X</c> from the theme keys
/// <c>Md.X.*</c>.
/// </summary>
public sealed class MdSourceLook
{
    private readonly List<(int Start, int Length, string Look)> _pieces = [];
    private readonly List<(int FirstLine, int LastLine, string Look)> _lines = [];

    internal MdSourceLook()
    {
    }

    /// <summary>Pieces of the source with their looks; a later piece is drawn over an earlier one.</summary>
    public IReadOnlyList<(int Start, int Length, string Look)> Pieces => _pieces;

    /// <summary>Source lines, counting from zero, with their looks: a background, a bar on the left.</summary>
    public IReadOnlyList<(int FirstLine, int LastLine, string Look)> Lines => _lines;

    /// <summary>
    /// Gives a piece of the source a look: <c>Heading1</c>–<c>Heading6</c>, <c>Bold</c>,
    /// <c>Italic</c>, <c>Strike</c>, <c>InlineCode</c>, <c>Link</c>, <c>CodeBlock</c>,
    /// <c>Source.Markup</c> or a name of your own.
    /// </summary>
    public void Mark(int start, int length, string look)
    {
        ArgumentNullException.ThrowIfNull(look);
        if (length > 0)
        {
            _pieces.Add((start, length, look));
        }
    }

    /// <summary>
    /// Gives source lines from <paramref name="firstLine"/> to <paramref name="lastLine"/> a look:
    /// <c>CodeBlock</c>, <c>Quote</c> or a name of your own.
    /// </summary>
    public void MarkLines(int firstLine, int lastLine, string look)
    {
        ArgumentNullException.ThrowIfNull(look);
        if (lastLine >= firstLine)
        {
            _lines.Add((firstLine, lastLine, look));
        }
    }

    /// <summary>Добавляет куски и строки другого описания после своих.</summary>
    internal void Append(MdSourceLook other)
    {
        _pieces.AddRange(other._pieces);
        _lines.AddRange(other._lines);
    }
}
