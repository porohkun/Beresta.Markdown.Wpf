namespace Beresta.Markdown.Wpf.Editor;

using Extensions;

/// <summary>
/// Описание исходника по строкам показанного текста: куски, разрезанные по строкам, и виды строк.
/// Места — в тексте <see cref="Text"/>.
/// </summary>
internal sealed class SourceLookIndex
{
    private readonly List<(int Start, int End, string Look)>?[] _pieces;
    private readonly List<(int FirstLine, int LastLine, string Look)>?[] _lines;
    private readonly Dictionary<int, int> _indents = [];

    public SourceLookIndex(MdSourceLook look, string text, TextLines lines)
    {
        Text = text;
        _pieces = new List<(int Start, int End, string Look)>?[lines.Count(text)];
        foreach (var (start, length, name) in look.Pieces)
        {
            var end = Math.Min(start + length, text.Length);
            for (var line = lines.LineAt(text, start); line < _pieces.Length && lines.StartOf(text, line) < end; line++)
            {
                var from = Math.Max(start, lines.StartOf(text, line));
                var to = Math.Min(end, lines.EndOf(text, line));
                if (to > from)
                {
                    (_pieces[line] ??= []).Add((from, to, name));
                }
            }
        }

        _lines = new List<(int FirstLine, int LastLine, string Look)>?[_pieces.Length];
        foreach (var entry in look.Lines)
        {
            for (var line = Math.Max(0, entry.FirstLine); line <= entry.LastLine && line < _lines.Length; line++)
            {
                (_lines[line] ??= []).Add(entry);
            }
        }

        Columns = look.Columns;
        foreach (var (line, length) in look.Indents)
        {
            _indents[line] = Math.Max(length, _indents.GetValueOrDefault(line));
        }
    }

    /// <summary>Текст, который описан.</summary>
    public string Text { get; }

    /// <summary>Таблицы, чьи столбцы выравниваются по разделителям.</summary>
    public IReadOnlyList<MdSourceColumns> Columns { get; }

    /// <summary>Куски строки с нуля в порядке рисования.</summary>
    public IReadOnlyList<(int Start, int End, string Look)> PiecesOn(int line)
        => line >= 0 && line < _pieces.Length ? _pieces[line] ?? [] : [];

    /// <summary>Длина маркера строки с нуля; маркера нет — 0.</summary>
    public int IndentOn(int line) => _indents.GetValueOrDefault(line);

    /// <summary>Виды строки с нуля и строки, которым их дали.</summary>
    public IReadOnlyList<(int FirstLine, int LastLine, string Look)> LooksAt(int line)
        => line >= 0 && line < _lines.Length ? _lines[line] ?? [] : [];

    /// <summary>Строки, которым дан вид <paramref name="look"/>: первая и последняя с нуля.</summary>
    public IEnumerable<(int FirstLine, int LastLine)> RangesOf(string look)
        => _lines
            .SelectMany(entries => entries ?? [])
            .Where(entry => entry.Look == look)
            .Select(entry => (entry.FirstLine, entry.LastLine))
            .Distinct();
}
