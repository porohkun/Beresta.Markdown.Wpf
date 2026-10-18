namespace Beresta.Markdown.Wpf.Kit;

using System.Text;

/// <summary>
/// Чтение CSV: значение в кавычках держит разделитель, перевод строки и <c>""</c> вместо кавычки.
/// Пробелы вокруг значения без кавычек отбрасываются, пустые строки между записями — тоже.
/// </summary>
internal static class CsvReader
{
    /// <summary>Записи по порядку: значения и номера первой и последней строки записи.</summary>
    public static List<(List<string> Fields, int First, int Last)> Read(IReadOnlyList<string> lines, char delimiter)
    {
        var records = new List<(List<string> Fields, int First, int Last)>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var quotedLength = -1;
        var first = -1;

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            if (!inQuotes && string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            first = first < 0 ? index : first;
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (inQuotes)
                {
                    if (c != '"')
                    {
                        field.Append(c);
                    }
                    else if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                        quotedLength = field.Length;
                    }
                }
                else if (c == delimiter)
                {
                    fields.Add(Take(field, quotedLength));
                    quotedLength = -1;
                }
                else if (c == '"' && quotedLength < 0 && IsBlank(field))
                {
                    field.Clear();
                    inQuotes = true;
                }
                else
                {
                    field.Append(c);
                }
            }

            if (inQuotes)
            {
                field.Append('\n');
                continue;
            }

            fields.Add(Take(field, quotedLength));
            quotedLength = -1;
            records.Add((fields, first, index));
            fields = [];
            first = -1;
        }

        if (first >= 0)
        {
            fields.Add(field.ToString().TrimEnd('\n'));
            records.Add((fields, first, lines.Count - 1));
        }

        return records;
    }

    /// <summary>Значение из накопленного: в кавычках — как есть, без кавычек — без пробелов по краям.</summary>
    private static string Take(StringBuilder field, int quotedLength)
    {
        var text = quotedLength < 0
            ? field.ToString().Trim()
            : field.ToString(0, quotedLength) + field.ToString(quotedLength, field.Length - quotedLength).TrimEnd();
        field.Clear();
        return text;
    }

    private static bool IsBlank(StringBuilder field)
    {
        for (var i = 0; i < field.Length; i++)
        {
            if (!char.IsWhiteSpace(field[i]))
            {
                return false;
            }
        }

        return true;
    }
}
