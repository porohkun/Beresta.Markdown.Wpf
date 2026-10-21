namespace Beresta.Markdown.Wpf.Kit;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ViewModels;

/// <summary>
/// Разбор front matter: <c>ключ: значение</c> верхнего уровня; значение — строка, список <c>[a, b]</c>
/// или <c>- a</c> через запятую, многострочное <c>|</c> и <c>&gt;</c>; остальное вложенное — текстом как
/// есть. Плоское значение и простой список можно записать заново тем же видом.
/// </summary>
internal static partial class FrontMatterProperties
{
    /// <summary>Знаки, с которых простое значение начинаться не может.</summary>
    private const string Indicators = "-?:,[]{}#&*!|>'\"%@`";

    /// <summary>
    /// Свойства по порядку: ключ, значение текстом, номера первой и последней строки свойства и место
    /// значения; значение, которое только показывается, места не имеет.
    /// </summary>
    public static List<(string Key, string Value, int First, int Last, FrontMatterValue? Place)> Read(IReadOnlyList<string> lines)
    {
        var properties = new List<(string Key, string Value, int First, int Last, FrontMatterValue? Place)>();
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

            nested = nested.GetRange(0, last - first);
            properties.Add(Property().Match(line) is { Success: true } match
                ? (Unquote(match.Groups["key"].Value), Value(match.Groups["value"].Value, nested), first, last, PlaceOf(lines, first, last, match, nested))
                : (string.Empty, string.Join("\n", [line.Trim(), .. Dedent(nested)]), first, last, null));
        }

        return properties;
    }

    /// <summary>
    /// Правка исходника: новое значение встаёт на место прежнего тем же видом. Простое остаётся простым,
    /// пока это безопасно, иначе встаёт в двойные кавычки; список в поле — пункты через запятую.
    /// </summary>
    /// <param name="place">Место и вид прежнего значения.</param>
    /// <param name="span">Место значения в исходнике.</param>
    /// <param name="text">Новое значение, как в поле.</param>
    /// <param name="source">Исходник: из него берётся перевод строки.</param>
    public static MdSourceEdit? Write(FrontMatterValue place, MdSourceSpan span, string text, string source)
    {
        var newLine = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var old = Elements(place.Kind == MdValueKind.BlockList ? BlockItems(place.Raw) : place.Raw.Trim('[', ']'));
        var value = place.Kind switch
        {
            MdValueKind.Plain => text.Length == 0 ? string.Empty : " " + Scalar(text, place.Raw, flow: false),
            MdValueKind.SingleQuoted => " " + (text.Contains('\n') ? Quote(text) : "'" + text.Replace("'", "''") + "'"),
            MdValueKind.DoubleQuoted => " " + Quote(text),
            MdValueKind.FlowList => " [" + string.Join(", ", Elements(text).Select((item, index) => Scalar(item, old.ElementAtOrDefault(index), flow: true))) + "]",
            MdValueKind.BlockList => string.Concat(Elements(text).Select((item, index) => newLine + place.Indent + "- " + Scalar(item, old.ElementAtOrDefault(index), flow: false))),
            _ => null
        };

        return value is null ? null : new MdSourceEdit(span.Start, span.Length, value);
    }

    /// <summary>
    /// Место значения, которое можно записать заново: строка, строка в кавычках, список в скобках или
    /// строками; вложенное, многострочное и с якорями — <c>null</c>.
    /// </summary>
    private static FrontMatterValue? PlaceOf(IReadOnlyList<string> lines, int first, int last, Match match, List<string> nested)
    {
        var line = lines[first];
        var start = line.IndexOf(':', match.Groups["key"].Index + match.Groups["key"].Length) + 1;
        var value = match.Groups["value"];
        var meaningful = nested
            .Where(text => text.Trim() is { Length: > 0 } trimmed && !trimmed.StartsWith('#'))
            .ToList();

        if (!value.Success || value.Length == 0)
        {
            if (meaningful.Count == 0)
            {
                return new(MdValueKind.Plain, first, start, first, line.Length, string.Empty, string.Empty);
            }

            var indents = meaningful
                .Select(text => text[..(text.Length - text.TrimStart().Length)])
                .Distinct()
                .ToList();
            return indents.Count == 1 && meaningful.All(text => text.TrimStart() is "-" || text.TrimStart().StartsWith("- ", StringComparison.Ordinal))
                ? new(MdValueKind.BlockList, first, start, last, lines[last].TrimEnd().Length, string.Join("\n", meaningful), indents[0])
                : null;
        }

        if (meaningful.Count > 0)
        {
            return null;
        }

        var raw = value.Value;
        switch (raw[0])
        {
            case '"' or '\'':
            {
                var quoted = QuotedPrefix().Match(raw).Value;
                return quoted.Length > 0 && Comment().Replace(raw[quoted.Length..], string.Empty).Trim().Length == 0
                    ? new(raw[0] == '"' ? MdValueKind.DoubleQuoted : MdValueKind.SingleQuoted, first, start, first, value.Index + quoted.Length, quoted, string.Empty)
                    : null;
            }

            case '[':
            {
                var list = Comment().Replace(raw, string.Empty).TrimEnd();
                return list.EndsWith(']') && list[1..^1].IndexOfAny(['[', ']', '{', '}']) < 0
                    ? new(MdValueKind.FlowList, first, start, first, value.Index + list.Length, list, string.Empty)
                    : null;
            }

            case '|' or '>' or '{' or '&' or '*' or '!':
                return null;

            default:
            {
                var plain = Comment().Replace(raw, string.Empty).TrimEnd();
                return new(MdValueKind.Plain, first, start, first, value.Index + plain.Length, plain, string.Empty);
            }
        }
    }

    /// <summary>Значение тем видом, каким оно безопасно: простое — как есть, иначе — в двойных кавычках.</summary>
    private static string Scalar(string text, string? old, bool flow)
        => Safe(text, old, flow) ? text : Quote(text);

    /// <summary>
    /// Простое значение прочтётся тем же текстом: не пустое, без пробелов по краям, без <c>: </c> и
    /// <c> #</c>, без знака разметки YAML в начале; похожее на флаг, пустоту или число — только если
    /// прежнее было таким же.
    /// </summary>
    private static bool Safe(string text, string? old, bool flow)
        => text.Length > 0
           && text == text.Trim()
           && !text.Contains('\n')
           && !text.Contains(": ", StringComparison.Ordinal)
           && !text.EndsWith(':')
           && !text.Contains(" #", StringComparison.Ordinal)
           && !Indicators.Contains(text[0])
           && (!flow || text.IndexOfAny([',', '[', ']', '{', '}']) < 0)
           && (!LooksTyped(text) || (old is not null && LooksTyped(old)));

    /// <summary>YAML прочтёт текст флагом, пустотой или числом.</summary>
    private static bool LooksTyped(string text)
        => text.ToLowerInvariant() is "true" or "false" or "null" or "~" or "yes" or "no" or "on" or "off"
           || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);

    private static string Quote(string text)
        => "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\t", "\\t") + "\"";

    /// <summary>Пункты из поля: через запятую, без пробелов по краям и пустых.</summary>
    private static List<string> Elements(string text)
        => text.Split(',')
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToList();

    /// <summary>Пункты списка строками через запятую — для сравнения с новыми.</summary>
    private static string BlockItems(string raw)
        => string.Join(", ", raw.Split('\n').Select(line => line.TrimStart()[1..].Trim()));

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
