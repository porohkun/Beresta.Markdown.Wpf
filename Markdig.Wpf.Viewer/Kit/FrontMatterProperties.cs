namespace MarkdigWpfViewer.Kit;

using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Разбор front matter только для показа: <c>ключ: значение</c> верхнего уровня; значение — строка,
/// список <c>[a, b]</c> или <c>- a</c> через запятую, многострочное <c>|</c> и <c>&gt;</c>; остальное
/// вложенное — текстом как есть.
/// </summary>
internal static partial class FrontMatterProperties
{
    /// <summary>Свойства по порядку: ключ, значение текстом, номера первой и последней строки свойства.</summary>
    public static List<(string Key, string Value, int First, int Last)> Read(IReadOnlyList<string> lines)
    {
        var properties = new List<(string Key, string Value, int First, int Last)>();
        var index = 0;
        while (index < lines.Count)
        {
            var line = lines[index];
            if (line.Trim() is "" || line.StartsWith('#'))
            {
                index++;
                continue;
            }

            var first = index;
            var last = index;
            var nested = new List<string>();
            for (index++; index < lines.Count && IsNested(lines[index]); index++)
            {
                nested.Add(lines[index]);
                last = lines[index].Trim().Length > 0 ? index : last;
            }

            nested = nested[..(last - first)];
            properties.Add(Property().Match(line) is { Success: true } match
                ? (Unquote(match.Groups["key"].Value), Value(match.Groups["value"].Value, nested), first, last)
                : (string.Empty, string.Join("\n", [line.Trim(), .. Dedent(nested)]), first, last));
        }

        return properties;
    }

    /// <summary>Строка принадлежит свойству выше: пустая, с отступом или пункт списка без отступа.</summary>
    private static bool IsNested(string line)
        => line.Length == 0 || char.IsWhiteSpace(line[0]) || line == "-" || line.StartsWith("- ", StringComparison.Ordinal);

    private static string Value(string value, List<string> nested)
    {
        if (value.StartsWith('|') || value.StartsWith('>'))
        {
            var lines = Dedent(nested);
            return value.StartsWith('|') ? string.Join("\n", lines) : Fold(lines);
        }

        var meaningful = nested
            .Where(line => line.Trim() is { Length: > 0 } text && !text.StartsWith('#'))
            .ToList();
        if (value.Length > 0)
        {
            return string.Join(" ", [Scalar(value), .. meaningful.Select(line => line.Trim())]);
        }

        var indents = meaningful
            .Select(line => line.Length - line.TrimStart().Length)
            .Distinct()
            .Count();
        return meaningful.Count > 0 && indents == 1 && meaningful.All(line => line.TrimStart() is "-" || line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
            ? string.Join(", ", meaningful.Select(line => Scalar(line.TrimStart()[1..].Trim())))
            : string.Join("\n", Dedent(meaningful));
    }

    /// <summary>Значение в одну строку: без кавычек и комментария; список <c>[a, b]</c> — через запятую.</summary>
    private static string Scalar(string value)
    {
        value = value.Trim();
        if (value.StartsWith('"') || value.StartsWith('\''))
        {
            return Unquote(QuotedPrefix().Match(value).Value is { Length: > 0 } quoted ? quoted : value);
        }

        value = Comment().Replace(value, string.Empty);
        return value.StartsWith('[') && value.EndsWith(']')
            ? string.Join(", ", Items(value[1..^1]).Select(Scalar).Where(item => item.Length > 0))
            : value;
    }

    /// <summary>Пункты списка через запятую; запятые внутри кавычек и скобок не делят.</summary>
    private static IEnumerable<string> Items(string list)
    {
        var depth = 0;
        var quote = '\0';
        var start = 0;
        for (var i = 0; i < list.Length; i++)
        {
            var c = list[i];
            if (quote != '\0')
            {
                quote = c == quote ? '\0' : quote;
            }
            else if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (c is '[' or '{')
            {
                depth++;
            }
            else if (c is ']' or '}')
            {
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                yield return list[start..i];
                start = i + 1;
            }
        }

        yield return list[start..];
    }

    private static string Unquote(string text)
    {
        if (text.Length >= 2 && text[0] == '\'' && text[^1] == '\'')
        {
            return text[1..^1].Replace("''", "'");
        }

        if (text.Length < 2 || text[0] != '"' || text[^1] != '"')
        {
            return text;
        }

        var result = new StringBuilder();
        for (var i = 1; i < text.Length - 1; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length - 1)
            {
                i++;
                result.Append(text[i] switch
                {
                    'n' => '\n',
                    't' => '\t',
                    var other => other
                });
            }
            else
            {
                result.Append(text[i]);
            }
        }

        return result.ToString();
    }

    /// <summary>Строки без общего отступа, без пустых в конце.</summary>
    private static List<string> Dedent(List<string> lines)
    {
        var indent = lines
            .Where(line => line.Trim().Length > 0)
            .Select(line => line.Length - line.TrimStart().Length)
            .DefaultIfEmpty(0)
            .Min();
        return lines
            .Select(line => line.Length > indent ? line[indent..].TrimEnd() : string.Empty)
            .Reverse()
            .SkipWhile(line => line.Length == 0)
            .Reverse()
            .ToList();
    }

    /// <summary>Многострочное <c>&gt;</c>: строки подряд — через пробел, пустая строка — перенос.</summary>
    private static string Fold(List<string> lines)
    {
        var text = new StringBuilder();
        var joined = false;
        foreach (var line in lines)
        {
            if (line.Length == 0)
            {
                text.Append('\n');
                joined = false;
                continue;
            }

            text.Append(joined ? " " : string.Empty).Append(line);
            joined = true;
        }

        return text.ToString();
    }

    [GeneratedRegex("""^(?<key>"(?:[^"\\]|\\.)*"|'(?:[^']|'')*'|(?!-(?:\s|$))[^\s#][^:#]*?)\s*:(?:\s+(?<value>.*?))?\s*$""")]
    private static partial Regex Property();

    [GeneratedRegex("""^(?:"(?:[^"\\]|\\.)*"|'(?:[^']|'')*')""")]
    private static partial Regex QuotedPrefix();

    [GeneratedRegex(@"\s+#.*$")]
    private static partial Regex Comment();
}
