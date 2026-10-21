namespace Beresta.Markdown.Wpf.Extensions;

using ViewModels;

/// <summary>
/// How the source of a document looks in an editor: named looks of pieces of the source text and of
/// source lines. An editor takes the fonts and colors of a look named <c>X</c> from the theme keys
/// <c>Md.X.*</c>.
/// </summary>
public sealed class MdSourceLook
{
    private readonly List<(int Start, int Length, string Look)> _pieces = [];
    private readonly List<(int FirstLine, int LastLine, string Look)> _lines = [];
    private readonly List<(int Line, int Length)> _indents = [];
    private readonly List<MdSourceColumns> _columns = [];

    internal MdSourceLook(string text)
    {
        Text = text;
    }

    /// <summary>The shown source text the places are in: an extension finds its marks in it.</summary>
    public string Text { get; }

    /// <summary>Pieces of the source with their looks; a later piece is drawn over an earlier one.</summary>
    public IReadOnlyList<(int Start, int Length, string Look)> Pieces => _pieces;

    /// <summary>Source lines, counting from zero, with their looks: a background, a bar on the left.</summary>
    public IReadOnlyList<(int FirstLine, int LastLine, string Look)> Lines => _lines;

    /// <summary>Source lines, counting from zero, that start with a marker of this length: wrapped lines start after it.</summary>
    public IReadOnlyList<(int Line, int Length)> Indents => _indents;

    /// <summary>Tables whose columns an editor lines up by their separators.</summary>
    public IReadOnlyList<MdSourceColumns> Columns => _columns;

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

    /// <summary>
    /// Marks the first <paramref name="length"/> characters of a source line as its marker — a list
    /// marker, <c>&gt;</c> of a quote: the wrapped lines of this line start after it.
    /// </summary>
    public void MarkIndent(int line, int length)
    {
        if (length > 0)
        {
            _indents.Add((line, length));
        }
    }

    /// <summary>
    /// Marks a table whose columns an editor lines up: the rows are source lines with the places of the
    /// separators that end their cells, the alignments are the alignments of the columns.
    /// </summary>
    public void MarkColumns(IReadOnlyList<(int Line, IReadOnlyList<int> Separators)> rows, IReadOnlyList<TableAlignment> alignments)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(alignments);
        if (rows.Count > 0)
        {
            _columns.Add(new(rows, alignments));
        }
    }

    /// <summary>Добавляет куски, строки, маркеры и столбцы другого описания после своих.</summary>
    internal void Append(MdSourceLook other)
    {
        _pieces.AddRange(other._pieces);
        _lines.AddRange(other._lines);
        _indents.AddRange(other._indents);
        _columns.AddRange(other._columns);
    }
}
