namespace Beresta.Markdown.Wpf.Editor;

/// <summary>
/// Строки текста, который показывает вьюер: переводы строк те же, что у Markdig и AvalonEdit, —
/// <c>\n</c>, <c>\r\n</c> и одиночный <c>\r</c>. Начала строк считаются заново, только когда текст
/// сменился.
/// </summary>
internal sealed class TextLines
{
    private string? _text;
    private List<int> _starts = [0];

    /// <summary>Сколько строк в тексте.</summary>
    public int Count(string text) => Starts(text).Count;

    /// <summary>Строка с нуля, в которой стоит место текста.</summary>
    public int LineAt(string text, int offset)
    {
        var starts = Starts(text);
        var found = starts.BinarySearch(Math.Clamp(offset, 0, text.Length));
        return found >= 0 ? found : ~found - 1;
    }

    /// <summary>Начало строки с нуля.</summary>
    public int StartOf(string text, int line)
    {
        var starts = Starts(text);
        return starts[Math.Clamp(line, 0, starts.Count - 1)];
    }

    /// <summary>Конец строки с нуля — без перевода строки.</summary>
    public int EndOf(string text, int line)
    {
        var starts = Starts(text);
        var index = Math.Clamp(line, 0, starts.Count - 1);
        var end = index + 1 < starts.Count ? starts[index + 1] : text.Length;
        while (end > starts[index] && text[end - 1] is '\r' or '\n')
        {
            end--;
        }

        return end;
    }

    private List<int> Starts(string text)
    {
        if (ReferenceEquals(text, _text))
        {
            return _starts;
        }

        var starts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n' || (text[i] == '\r' && (i + 1 == text.Length || text[i + 1] != '\n')))
            {
                starts.Add(i + 1);
            }
        }

        _text = text;
        _starts = starts;
        return starts;
    }
}
