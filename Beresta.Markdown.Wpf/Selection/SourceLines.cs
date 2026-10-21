namespace Beresta.Markdown.Wpf.Selection;

/// <summary>Строки исходного текста документа без знаков конца строки, считая с нуля.</summary>
internal sealed class SourceLines
{
    private readonly string _text;
    private readonly List<int> _starts = [0];

    public SourceLines(string text)
    {
        _text = text;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n' || (text[i] == '\r' && (i + 1 == text.Length || text[i + 1] != '\n')))
            {
                _starts.Add(i + 1);
            }
        }
    }

    public int Count => _starts.Count;

    /// <summary>Текст исходника.</summary>
    public string Text => _text;

    /// <summary>Начало строки в тексте исходника.</summary>
    public int StartOf(int line) => _starts[line];

    /// <summary>Текст строки; нет такой строки — <c>null</c>.</summary>
    public string? TextOf(int line) => line >= 0 && line < _starts.Count ? Span(line).ToString() : null;

    /// <summary>Совпадает ли строка с текстом; <c>null</c> совпадает только с несуществующей строкой.</summary>
    public bool Is(int line, string? text)
    {
        if (line < 0 || line >= _starts.Count)
        {
            return text is null;
        }

        return text is not null && Span(line).SequenceEqual(text);
    }

    private ReadOnlySpan<char> Span(int line)
    {
        var start = _starts[line];
        var end = line + 1 < _starts.Count ? _starts[line + 1] : _text.Length;
        return _text.AsSpan(start, end - start).TrimEnd("\r\n");
    }
}
