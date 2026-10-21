namespace Beresta.Markdown.Wpf.Editor;

using Extensions;

/// <summary>
/// Описание исходника по строкам показанного текста: куски, разрезанные по строкам, и виды строк.
/// Места — в тексте <see cref="Text"/>.
/// </summary>
internal sealed class SourceLookIndex
{
    private readonly List<(int Start, int End, string Look)>?[] _pieces;
    private readonly IReadOnlyList<(int FirstLine, int LastLine, string Look)> _lines;

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

        _lines = look.Lines;
    }

    /// <summary>Текст, который описан.</summary>
    public string Text { get; }

    /// <summary>Куски строки с нуля в порядке рисования.</summary>
    public IReadOnlyList<(int Start, int End, string Look)> PiecesOn(int line)
        => line >= 0 && line < _pieces.Length ? _pieces[line] ?? [] : [];

    /// <summary>Виды строки с нуля и строки, которым их дали.</summary>
    public IEnumerable<(int FirstLine, int LastLine, string Look)> LooksAt(int line)
        => _lines.Where(entry => entry.FirstLine <= line && line <= entry.LastLine);
}
