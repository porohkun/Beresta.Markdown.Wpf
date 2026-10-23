namespace Beresta.Markdown.Wpf.Editor;

using System.Windows;
using System.Windows.Media;
using Extensions;

/// <summary>
/// Поля строк блоков кода и таблиц исходника: блок встаёт высотой нарисованного, строка кода — у своего
/// места в нём, ряды таблицы — через равные промежутки.
/// </summary>
/// <param name="find">Ресурс по ключу; нет — <c>null</c>.</param>
/// <param name="font">Шрифт AvalonEdit.</param>
/// <param name="size">Размер шрифта AvalonEdit.</param>
/// <param name="lineHeight">Высота строки AvalonEdit.</param>
/// <param name="round">Размер в отрисовке после округления раскладки.</param>
internal sealed class SourceFit(Func<string, object?> find, FontFamily font, double size, double lineHeight, Func<double, double> round)
{
    /// <summary>Поля над текстом и под ним у строк описанного текста с нуля, которым они нужны.</summary>
    public Dictionary<int, (double Above, double Below)> Of(SourceLookIndex look, TextLines lines)
    {
        var fits = new Dictionary<int, (double Above, double Below)>();
        foreach (var (first, last) in look.RangesOf("CodeBlock"))
        {
            Put(fits, first, Code(look, lines, first, last));
        }

        foreach (var table in look.Columns)
        {
            if (Table(look, lines, table) is { } rows)
            {
                Put(fits, table.Rows[0].Line, rows);
            }
        }

        return fits;
    }

    /// <summary>
    /// Поля строк блока высотой <paramref name="total"/>: строка встаёт к своему месту, но не выше конца
    /// предыдущей, а не помещается блок — строки с конца поднимаются на поля над ними.
    /// </summary>
    /// <param name="lines">Высота текста строки и место верха текста в блоке; строке без места в отрисовке — <c>null</c>.</param>
    /// <param name="total">Высота нарисованного блока.</param>
    public static (double Above, double Below)[] Fit(IReadOnlyList<(double Height, double? Top)> lines, double total)
    {
        var tops = new double[lines.Count];
        var end = 0.0;
        for (var line = 0; line < lines.Count; line++)
        {
            tops[line] = Math.Max(lines[line].Top ?? end, end);
            end = tops[line] + lines[line].Height;
        }

        if (end > total)
        {
            var bottom = total;
            for (var line = lines.Count - 1; line >= 0; line--)
            {
                tops[line] = Math.Min(tops[line], bottom - lines[line].Height);
                bottom = tops[line];
            }

            // Тексту строк в блоке тесно: полей нет
            if (bottom < 0)
            {
                var top = 0.0;
                for (var line = 0; line < lines.Count; line++)
                {
                    tops[line] = top;
                    top += lines[line].Height;
                }
            }
        }

        var fits = new (double Above, double Below)[lines.Count];
        var previous = 0.0;
        for (var line = 0; line < lines.Count; line++)
        {
            fits[line] = (Math.Max(0, tops[line] - previous), 0);
            previous = tops[line] + lines[line].Height;
        }

        if (lines.Count > 0)
        {
            fits[^1] = fits[^1] with { Below = Math.Max(0, total - previous) };
        }

        return fits;
    }

    /// <summary>
    /// Блок кода: у ограждённого над кодом — шапка с языком, вокруг кода — рамка и внутреннее поле; строка
    /// кода высотой строки своего шрифта, ограждение — строки AvalonEdit.
    /// </summary>
    private (double Above, double Below)[] Code(SourceLookIndex look, TextLines lines, int first, int last)
    {
        var border = find("Md.CodeBlock.BorderThickness") as Thickness? ?? default;
        var padding = find("Md.CodeBlock.Padding") as Thickness? ?? default;
        var code = (find("Md.CodeBlock.FontFamily") as FontFamily ?? font).LineSpacing * (find("Md.CodeBlock.FontSize") as double? ?? size);
        var opened = IsFence(first);
        var closed = last > first && IsFence(last);
        var top = round(border.Top) + round(padding.Top);
        var rows = new List<(double Height, double? Top)>();
        if (opened)
        {
            var margin = find("Md.CodeBlock.HeaderMargin") as Thickness? ?? default;
            var header = (find("Md.CodeBlock.HeaderFontFamily") as FontFamily ?? font).LineSpacing * (find("Md.CodeBlock.HeaderFontSize") as double? ?? size);
            rows.Add((lineHeight, top + round(margin.Top)));
            top += round(margin.Top) + round(header) + round(margin.Bottom);
        }

        var count = last - first + 1 - (opened ? 1 : 0) - (closed ? 1 : 0);
        for (var row = 0; row < count; row++)
        {
            rows.Add((code, top + (row * code)));
        }

        if (closed)
        {
            rows.Add((lineHeight, null));
        }

        return Fit(rows, top + round(count * code) + round(padding.Bottom) + round(border.Bottom));

        bool IsFence(int line)
            => !look.PiecesOn(line).Any(piece => piece.Look == "CodeBlock") && !Text(look, lines, line).IsWhiteSpace();
    }

    /// <summary>
    /// Таблица со строкой-разделителем вторым рядом: поля рядов поровну до высоты нарисованной таблицы;
    /// разделителя нет или ряды идут не подряд — <c>null</c>.
    /// </summary>
    private (double Above, double Below)[]? Table(SourceLookIndex look, TextLines lines, MdSourceColumns table)
    {
        var rows = table.Rows;
        if (rows.Count < 2
            || !IsDelimiter(Text(look, lines, rows[1].Line))
            || Enumerable.Range(1, rows.Count - 1).Any(index => rows[index].Line != rows[index - 1].Line + 1))
        {
            return null;
        }

        var border = find("Md.Table.BorderThickness") as Thickness? ?? default;
        var padding = find("Md.Table.CellPadding") as Thickness? ?? default;
        var drawn = rows.Count - 1;
        var row = round(border.Top) + round(padding.Top) + round(lineHeight) + round(padding.Bottom) + round(border.Bottom);
        var total = round(border.Top) + (drawn * row) + round(border.Bottom);
        var half = Math.Max(0, total - (rows.Count * lineHeight)) / drawn / 2;
        return Enumerable.Range(0, rows.Count)
            .Select(index => index == 1 ? (0.0, 0.0) : (half, half))
            .ToArray();

        static bool IsDelimiter(ReadOnlySpan<char> text)
            => text.Contains('-') && text.IndexOfAnyExcept("|-: \t>") < 0;
    }

    /// <summary>Текст строки с нуля без перевода строки.</summary>
    private static ReadOnlySpan<char> Text(SourceLookIndex look, TextLines lines, int line)
    {
        var start = lines.StartOf(look.Text, line);
        return look.Text.AsSpan(start, lines.EndOf(look.Text, line) - start);
    }

    /// <summary>Поля строк блока с первой <paramref name="first"/>; строки без полей не записываются.</summary>
    private static void Put(Dictionary<int, (double Above, double Below)> fits, int first, (double Above, double Below)[] rows)
    {
        for (var row = 0; row < rows.Length; row++)
        {
            if (rows[row] != default)
            {
                fits[first + row] = rows[row];
            }
        }
    }
}
