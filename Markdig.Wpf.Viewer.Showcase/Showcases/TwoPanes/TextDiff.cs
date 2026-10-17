namespace MarkdigWpfViewer.Showcase.Showcases.TwoPanes;

using System.Text.RegularExpressions;

/// <summary>Дифф внутри строк по словам: куски текста, которые дописаны, удалены или заменены.</summary>
public static partial class TextDiff
{
    /// <summary>
    /// Правки между совпавшими словами: начало и длина куска слева и справа; кусок нулевой длины — место,
    /// где на другой стороне есть текст. Правка из одних пробелов не в счёт.
    /// </summary>
    public static IEnumerable<(int LeftStart, int LeftLength, int RightStart, int RightLength)> Pieces(string left, string right)
    {
        var leftTokens = Tokens(left);
        var rightTokens = Tokens(right);
        var matches = LineDiff.Match(Comparable(leftTokens), Comparable(rightTokens));

        var (i, j) = (0, 0);
        foreach (var (matchLeft, matchRight) in matches.Append((leftTokens.Count, rightTokens.Count)))
        {
            var (leftStart, leftEnd) = Piece(leftTokens, i, matchLeft, left.Length);
            var (rightStart, rightEnd) = Piece(rightTokens, j, matchRight, right.Length);
            if (!string.IsNullOrWhiteSpace(left[leftStart..leftEnd]) || !string.IsNullOrWhiteSpace(right[rightStart..rightEnd]))
            {
                yield return (leftStart, leftEnd - leftStart, rightStart, rightEnd - rightStart);
            }

            (i, j) = (matchLeft + 1, matchRight + 1);
        }
    }

    /// <summary>Слова, пробелы и отдельные знаки по порядку.</summary>
    private static List<Match> Tokens(string text) => TokenPattern().Matches(text).ToList();

    /// <summary>Слова для сравнения: любой пробел равен любому.</summary>
    private static List<string> Comparable(List<Match> tokens)
        => tokens.Select(token => string.IsNullOrWhiteSpace(token.Value) ? " " : token.Value).ToList();

    /// <summary>Кусок текста слов с <paramref name="from"/> до <paramref name="to"/>; без слов — место перед словом <paramref name="from"/>.</summary>
    private static (int Start, int End) Piece(List<Match> tokens, int from, int to, int length)
    {
        var start = from < tokens.Count ? tokens[from].Index : length;
        return to > from ? (start, tokens[to - 1].Index + tokens[to - 1].Length) : (start, start);
    }

    [GeneratedRegex(@"\w+|\s+|[^\w\s]")]
    private static partial Regex TokenPattern();
}
