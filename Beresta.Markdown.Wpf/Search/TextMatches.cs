namespace Beresta.Markdown.Wpf.Search;

using Selection;

/// <summary>Совпадения запроса в тексте карты.</summary>
internal static class TextMatches
{
    /// <summary>
    /// Совпадения без наложения, по порядку; без учёта регистра — по правилам без культуры. Слово
    /// целиком — по обе стороны не буква, не цифра и не подчёркивание.
    /// </summary>
    public static List<MdTextRange> Find(string text, string? query, bool matchCase, bool wholeWord)
    {
        var found = new List<MdTextRange>();
        if (string.IsNullOrEmpty(query))
        {
            return found;
        }

        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var from = 0;
        while (from <= text.Length - query.Length)
        {
            var index = text.IndexOf(query, from, comparison);
            if (index < 0)
            {
                break;
            }

            if (wholeWord && !IsWholeWord(text, index, query.Length))
            {
                from = index + 1;
                continue;
            }

            found.Add(new(index, query.Length));
            from = index + query.Length;
        }

        return found;
    }

    private static bool IsWholeWord(string text, int index, int length)
        => (index == 0 || !IsWordSymbol(text[index - 1]))
           && (index + length == text.Length || !IsWordSymbol(text[index + length]));

    private static bool IsWordSymbol(char symbol) => char.IsLetterOrDigit(symbol) || symbol == '_';
}
