namespace Beresta.Markdown.Wpf.Editor;

using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

/// <summary>
/// Клавиши правки Markdown: Enter продолжает список и выходит из него, Tab и Shift+Tab сдвигают пункт,
/// набранное ограждение кода закрывается само, вставка — только текстом. Каждое правило — один шаг
/// отмены; внутри блока кода правила списка не работают.
/// </summary>
internal static partial class EditingKeys
{
    /// <summary>Ширина табуляции в отступах пунктов.</summary>
    private const int TabWidth = 4;

    /// <summary>
    /// Enter в пункте списка: новый пункт с той же разметкой, а в пункте, где только маркер, — выход из
    /// списка. Не пункт — <c>false</c>: перевод строки делает AvalonEdit.
    /// </summary>
    public static bool Enter(TextArea area)
    {
        var document = area.Document;
        if (!area.Selection.IsEmpty)
        {
            return false;
        }

        var line = document.GetLineByOffset(area.Caret.Offset);
        if (Item().Match(document.GetText(line)) is not { Success: true } item
            || area.Caret.Offset < line.Offset + item.Groups["body"].Index
            || InCode(document, line.LineNumber))
        {
            return false;
        }

        using (document.RunUpdate())
        {
            if (string.IsNullOrWhiteSpace(item.Groups["body"].Value))
            {
                document.Remove(line.Offset, line.Length);
                area.Caret.Offset = line.Offset;
                return true;
            }

            var marker = item.Groups["number"].Success
                ? (long.Parse(item.Groups["number"].Value, CultureInfo.InvariantCulture) + 1).ToString(CultureInfo.InvariantCulture) + item.Groups["delimiter"].Value
                : item.Groups["marker"].Value;
            var task = item.Groups["task"].Success ? "[ ]" + item.Groups["after"].Value : string.Empty;
            var prefix = item.Groups["indent"].Value + marker + item.Groups["space"].Value + task;
            var caret = area.Caret.Offset;
            var newLine = TextUtilities.GetNewLineFromDocument(document, line.LineNumber);
            document.Insert(caret, newLine + prefix);
            area.Caret.Offset = caret + newLine.Length + prefix.Length;
        }

        return true;
    }

    /// <summary>
    /// Tab и Shift+Tab в пункте: пункт со всеми его строками встаёт туда, где начинается содержимое
    /// предыдущего пункта того же уровня, или туда, где начинается его родитель. Не пункт —
    /// <c>false</c>: Tab делает AvalonEdit.
    /// </summary>
    public static bool Shift(TextArea area, bool outdent)
    {
        var document = area.Document;
        var first = document.GetLineByOffset(area.Selection.IsEmpty ? area.Caret.Offset : area.Selection.SurroundingSegment.Offset);
        if (Item().Match(document.GetText(first)) is not { Success: true } item || InCode(document, first.LineNumber))
        {
            return false;
        }

        var indent = Width(item.Groups["indent"].Value);
        var target = outdent ? ParentIndent(document, first.LineNumber, indent) : SiblingText(document, first.LineNumber, indent);
        if (target is not { } column || column == indent)
        {
            return true;
        }

        var last = ItemEnd(document, first.LineNumber, indent);
        using (document.RunUpdate())
        {
            for (var number = first.LineNumber; number <= last; number++)
            {
                var line = document.GetLineByNumber(number);
                var text = document.GetText(line);
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (column > indent)
                {
                    document.Insert(line.Offset, new string(' ', column - indent));
                }
                else
                {
                    document.Remove(line.Offset, LeadingChars(text, indent - column));
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Набранное в начале строки ограждение кода закрывается строкой ниже, если оно открывает блок, а
    /// ограждения ниже разбиты на пары между собой; курсор остаётся после открывающего.
    /// </summary>
    public static void CloseFence(TextArea area, string typed)
    {
        if (typed is not ("`" or "~"))
        {
            return;
        }

        var document = area.Document;
        var line = document.GetLineByOffset(area.Caret.Offset);
        var text = document.GetText(line);
        if (area.Caret.Offset != line.EndOffset
            || Fence().Match(text) is not { Success: true } fence
            || fence.Length != text.Length
            || InCode(document, line.LineNumber))
        {
            return;
        }

        var mark = fence.Groups["fence"].Value;
        var below = 0;
        for (var number = line.LineNumber + 1; number <= document.LineCount; number++)
        {
            if (Fence().Match(document.GetText(document.GetLineByNumber(number))) is { Success: true } other
                && other.Groups["fence"].Value[0] == mark[0])
            {
                below++;
            }
        }

        if (below % 2 == 1)
        {
            return;
        }

        var caret = area.Caret.Offset;
        using (document.RunUpdate())
        {
            document.Insert(line.EndOffset, TextUtilities.GetNewLineFromDocument(document, line.LineNumber) + fence.Groups["indent"].Value + mark);
        }

        area.Caret.Offset = caret;
    }

    /// <summary>Вставка и перетаскивание — только текстом: без текста в данных ничего не вставляется.</summary>
    public static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.UnicodeText, true))
        {
            e.FormatToApply = DataFormats.UnicodeText;
        }
        else
        {
            e.CancelCommand();
        }
    }

    /// <summary>Пункт списка: отступ, маркер или номер с разделителем, пробелы, флажок задачи и текст.</summary>
    [GeneratedRegex(@"^(?<indent>[ \t]*)(?<marker>[-*+]|(?<number>\d{1,9})(?<delimiter>[.)]))(?<space>[ \t]+)(?:(?<task>\[[ xX]\])(?<after>[ \t]+))?(?<body>.*)$")]
    private static partial Regex Item();

    /// <summary>Строка ограждения кода: до трёх пробелов, три и больше обратных кавычек или тильд и сведения о языке.</summary>
    [GeneratedRegex(@"^(?<indent> {0,3})(?<fence>`{3,}|~{3,})")]
    private static partial Regex Fence();

    /// <summary>Строка — внутри ограждённого блока кода: ограждения выше открыли блок и не закрыли.</summary>
    private static bool InCode(TextDocument document, int number)
    {
        string? open = null;
        for (var current = 1; current < number; current++)
        {
            if (Fence().Match(document.GetText(document.GetLineByNumber(current))) is not { Success: true } fence)
            {
                continue;
            }

            var mark = fence.Groups["fence"].Value;
            if (open is null)
            {
                open = mark;
            }
            else if (mark[0] == open[0] && mark.Length >= open.Length)
            {
                open = null;
            }
        }

        return open is not null;
    }

    /// <summary>Где начинается содержимое предыдущего пункта того же уровня; такого нет — <c>null</c>.</summary>
    private static int? SiblingText(TextDocument document, int number, int indent)
    {
        for (var current = number - 1; current >= 1; current--)
        {
            var text = document.GetText(document.GetLineByNumber(current));
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var width = Width(LeadingWhitespace(text));
            if (Item().Match(text) is { Success: true } item && width <= indent)
            {
                var content = item.Groups["task"].Success ? item.Groups["task"].Index : item.Groups["body"].Index;
                return width == indent ? Width(text[..content]) : null;
            }

            if (width < indent)
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>Отступ пункта-родителя — ближайшего пункта выше с меньшим отступом; родителя нет — <c>null</c>.</summary>
    private static int? ParentIndent(TextDocument document, int number, int indent)
    {
        for (var current = number - 1; current >= 1; current--)
        {
            var text = document.GetText(document.GetLineByNumber(current));
            if (Item().Match(text) is { Success: true } && Width(LeadingWhitespace(text)) < indent)
            {
                return Width(LeadingWhitespace(text));
            }
        }

        return null;
    }

    /// <summary>Последняя строка пункта: следом идут строки с отступом больше, чем у пункта, и пустые между ними.</summary>
    private static int ItemEnd(TextDocument document, int number, int indent)
    {
        var last = number;
        for (var current = number + 1; current <= document.LineCount; current++)
        {
            var text = document.GetText(document.GetLineByNumber(current));
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (Width(LeadingWhitespace(text)) <= indent)
            {
                break;
            }

            last = current;
        }

        return last;
    }

    private static string LeadingWhitespace(string text) => text[..(text.Length - text.TrimStart(' ', '\t').Length)];

    /// <summary>Ширина отступа в колонках; табуляция — до следующей колонки, кратной её ширине.</summary>
    private static int Width(string text)
    {
        var width = 0;
        foreach (var symbol in text)
        {
            width = symbol == '\t' ? (width / TabWidth + 1) * TabWidth : width + 1;
        }

        return width;
    }

    /// <summary>Сколько знаков в начале строки занимают столько колонок отступа.</summary>
    private static int LeadingChars(string text, int columns)
    {
        var width = 0;
        var count = 0;
        while (count < text.Length && text[count] is ' ' or '\t' && width < columns)
        {
            width = text[count] == '\t' ? (width / TabWidth + 1) * TabWidth : width + 1;
            count++;
        }

        return count;
    }
}
