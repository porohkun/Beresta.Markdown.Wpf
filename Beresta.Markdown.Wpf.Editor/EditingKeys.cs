namespace Beresta.Markdown.Wpf.Editor;

using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

/// <summary>
/// Клавиши правки Markdown: Enter продолжает список и выходит из него, Tab и Shift+Tab сдвигают пункт,
/// Ctrl+B и Ctrl+I обрамляют знаками жирного и курсива, нумерованный список перенумеровывается, набранное
/// ограждение кода закрывается само, вставка — только текстом, а вставка с форматированием переводит
/// HTML в Markdown. Каждое правило — один шаг отмены; внутри блока кода правила списка не работают.
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
                ? NextNumber(document, line.LineNumber, item).ToString(CultureInfo.InvariantCulture) + item.Groups["delimiter"].Value
                : item.Groups["marker"].Value;
            var task = item.Groups["task"].Success ? "[ ]" + item.Groups["after"].Value : string.Empty;
            var prefix = item.Groups["indent"].Value + marker + item.Groups["space"].Value + task;
            var caret = area.Caret.Offset;
            var newLine = TextUtilities.GetNewLineFromDocument(document, line.LineNumber);
            document.Insert(caret, newLine + prefix);
            area.Caret.Offset = caret + newLine.Length + prefix.Length;
            Renumber(document, line.LineNumber + 1);
        }

        return true;
    }

    /// <summary>Номер нового пункта после пункта <paramref name="item"/>: следующий, а в списке с одним номером у всех пунктов — тот же.</summary>
    private static long NextNumber(TextDocument document, int number, Match item)
    {
        var current = long.Parse(item.Groups["number"].Value, CultureInfo.InvariantCulture);
        var siblings = Siblings(document, number);
        return siblings.Count > 1 && siblings.All(sibling => sibling.Number == current) ? current : current + 1;
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

            Renumber(document, first.LineNumber);
            if (NextAtIndent(document, last + 1, indent) is { } next)
            {
                Renumber(document, next);
            }
        }

        return true;
    }

    /// <summary>
    /// Backspace или Delete удалили строку: нумерованный список, на пункте которого остался курсор или
    /// перед пунктом которого он стоит, перенумеровывается тем же шагом отмены.
    /// </summary>
    public static void RenumberAfterDelete(TextArea area)
    {
        var document = area.Document;
        var number = document.GetLineByOffset(area.Caret.Offset).LineNumber;
        while (number < document.LineCount && string.IsNullOrWhiteSpace(document.GetText(document.GetLineByNumber(number))))
        {
            number++;
        }

        document.UndoStack.StartContinuedUndoGroup();
        try
        {
            using (document.RunUpdate())
            {
                Renumber(document, number);
            }
        }
        finally
        {
            document.UndoStack.EndUndoGroup();
        }
    }

    /// <summary>
    /// Ctrl+B и Ctrl+I: знаки <paramref name="mark"/> вокруг выделения или слова под курсором, а обрамлённое
    /// ими снаружи или изнутри их теряет; вне слова — пара знаков с курсором между ними. Выделение через
    /// несколько строк обрамляется построчно — без маркеров пунктов, цитат и заголовков, без пустых строк.
    /// Курсор и выделение остаются на том же тексте.
    /// </summary>
    public static void Emphasis(TextArea area, string mark)
    {
        var document = area.Document;
        var selection = area.Selection;
        var caret = area.Caret.Offset;
        var word = selection.IsEmpty ? Word(document, caret) : null;
        if (selection.IsEmpty && word is null)
        {
            Pair(area, mark);
            return;
        }

        var segments = word is { } around ? [around] : Segments(document, selection.SurroundingSegment.Offset, selection.SurroundingSegment.EndOffset);
        if (segments.Count == 0)
        {
            return;
        }

        var placed = Toggle(document, segments, mark);
        if (selection.IsEmpty)
        {
            area.Caret.Offset = placed[0].Start + (caret - segments[0].Start);
            return;
        }

        var (start, end) = (placed[0].Start, placed[^1].End);
        var atStart = caret == selection.SurroundingSegment.Offset;
        area.Selection = atStart ? Selection.Create(area, end, start) : Selection.Create(area, start, end);
        area.Caret.Offset = atStart ? start : end;
    }

    /// <summary>
    /// Пункт нумерованного списка на строке <paramref name="number"/> и пункты того же списка и уровня
    /// после него — номерами подряд от предыдущего пункта; первый пункт списка номер сохраняет. Разделитель
    /// и место текста остаются; список, где у всех пунктов один номер, не трогается.
    /// </summary>
    private static void Renumber(TextDocument document, int number)
    {
        var siblings = Siblings(document, number);
        var index = siblings.FindIndex(sibling => sibling.Line == number);
        if (index < 0 || siblings.All(sibling => sibling.Number == siblings[0].Number))
        {
            return;
        }

        var expected = index == 0 ? siblings[0].Number : siblings[index - 1].Number + 1;
        for (var at = index; at < siblings.Count; at++, expected++)
        {
            if (siblings[at].Number != expected)
            {
                Number(document, siblings[at].Line, expected);
            }
        }
    }

    /// <summary>Пункт на строке получает номер <paramref name="value"/>; текст пункта остаётся на своём месте, если пробелов хватает.</summary>
    private static void Number(TextDocument document, int number, long value)
    {
        var line = document.GetLineByNumber(number);
        var item = Item().Match(document.GetText(line));
        var digits = item.Groups["number"];
        var space = item.Groups["space"];
        var text = value.ToString(CultureInfo.InvariantCulture);
        var spaces = space.Value.Contains('\t') ? space.Value : new string(' ', Math.Max(1, digits.Length + space.Length - text.Length));
        document.Replace(line.Offset + digits.Index, space.Index + space.Length - digits.Index, text + item.Groups["delimiter"].Value + spaces);
    }

    /// <summary>
    /// Нумерованные пункты списка на строке <paramref name="number"/> того же уровня и с тем же
    /// разделителем, сверху вниз; на строке не нумерованный пункт — пусто.
    /// </summary>
    private static List<(int Line, long Number)> Siblings(TextDocument document, int number)
    {
        if (number < 1 || number > document.LineCount || Item().Match(document.GetText(document.GetLineByNumber(number))) is not { Success: true } item || !item.Groups["number"].Success)
        {
            return [];
        }

        var indent = Width(item.Groups["indent"].Value);
        var delimiter = item.Groups["delimiter"].Value;
        var siblings = new List<(int Line, long Number)> { (number, long.Parse(item.Groups["number"].Value, CultureInfo.InvariantCulture)) };
        for (var current = number - 1; current >= 1 && SiblingAt(document, current, indent, delimiter) is var found and not -1L; current--)
        {
            if (found is { } value)
            {
                siblings.Insert(0, (current, value));
            }
        }

        for (var current = number + 1; current <= document.LineCount && SiblingAt(document, current, indent, delimiter) is var found and not -1L; current++)
        {
            if (found is { } value)
            {
                siblings.Add((current, value));
            }
        }

        return siblings;
    }

    /// <summary>
    /// Строка рядом со списком: номер пункта того же уровня и разделителя; пустая строка или вложенное —
    /// <c>null</c>, список здесь кончается — <c>-1</c>.
    /// </summary>
    private static long? SiblingAt(TextDocument document, int number, int indent, string delimiter)
    {
        var text = document.GetText(document.GetLineByNumber(number));
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var width = Width(LeadingWhitespace(text));
        if (width > indent)
        {
            return null;
        }

        return width == indent && Item().Match(text) is { Success: true } item && item.Groups["number"].Success && item.Groups["delimiter"].Value == delimiter
            ? long.Parse(item.Groups["number"].Value, CultureInfo.InvariantCulture)
            : -1;
    }

    /// <summary>Первая непустая строка с <paramref name="number"/> и ниже, если это пункт с отступом <paramref name="indent"/>.</summary>
    private static int? NextAtIndent(TextDocument document, int number, int indent)
    {
        for (var current = number; current <= document.LineCount; current++)
        {
            var text = document.GetText(document.GetLineByNumber(current));
            if (!string.IsNullOrWhiteSpace(text))
            {
                return Item().IsMatch(text) && Width(LeadingWhitespace(text)) == indent ? current : null;
            }
        }

        return null;
    }

    /// <summary>Курсор вне слова: пара знаков вокруг него снимается, а нет её — ставится, и курсор между знаками.</summary>
    private static void Pair(TextArea area, string mark)
    {
        var document = area.Document;
        var caret = area.Caret.Offset;
        using (document.RunUpdate())
        {
            if (FrameOf(document, caret, caret, mark) is { } frame)
            {
                document.Remove(frame.Close, mark.Length);
                document.Remove(frame.Open, mark.Length);
                caret -= mark.Length;
            }
            else
            {
                document.Insert(caret, mark + mark);
                caret += mark.Length;
            }
        }

        area.Caret.Offset = caret;
    }

    /// <summary>
    /// Куски, обрамлённые знаками все, их теряют, иначе необрамлённые обрамляются — одним шагом отмены.
    /// Возвращает места текста кусков после правки.
    /// </summary>
    private static List<(int Start, int End)> Toggle(TextDocument document, IReadOnlyList<(int Start, int End)> segments, string mark)
    {
        var length = mark.Length;
        var frames = segments.Select(segment => FrameOf(document, segment.Start, segment.End, mark)).ToList();
        var remove = frames.All(frame => frame is not null);
        var placed = new List<(int Start, int End)>();
        var shift = 0;
        for (var index = 0; index < segments.Count; index++)
        {
            var (start, end) = segments[index];
            if (remove && frames[index] is { } frame)
            {
                placed.Add((Math.Max(start, frame.Open + length) - length + shift, Math.Min(end, frame.Close) - length + shift));
                shift -= 2 * length;
            }
            else if (frames[index] is null)
            {
                placed.Add((start + length + shift, end + length + shift));
                shift += 2 * length;
            }
            else
            {
                placed.Add((start + shift, end + shift));
            }
        }

        using (document.RunUpdate())
        {
            for (var index = segments.Count - 1; index >= 0; index--)
            {
                if (remove && frames[index] is { } frame)
                {
                    document.Remove(frame.Close, length);
                    document.Remove(frame.Open, length);
                }
                else if (frames[index] is null)
                {
                    document.Insert(segments[index].End, mark);
                    document.Insert(segments[index].Start, mark);
                }
            }
        }

        return placed;
    }

    /// <summary>
    /// Где у куска знаки <paramref name="mark"/>, открывающие и закрывающие, — снаружи куска или у его
    /// края; не обрамлён — <c>null</c>. Обрамление — <c>*</c> или <c>_</c> подряд с обеих сторон: у
    /// жирного их хотя бы два, у курсива — нечётное число.
    /// </summary>
    private static (int Open, int Close)? FrameOf(TextDocument document, int start, int end, string mark)
    {
        var length = mark.Length;
        var before = Run(document, start - 1, -1, -1);
        var (open, opening) = before.Count > 0 ? (start - length, before) : (start, Run(document, start, 1, end));
        var after = Run(document, end, 1, document.TextLength);
        var (close, closing) = after.Count > 0 ? (end, after) : (end - length, Run(document, end - 1, -1, open + length - 1));
        return Frames(opening, closing, mark) && close >= open + length ? (open, close) : null;
    }

    private static bool Frames((char Symbol, int Count) before, (char Symbol, int Count) after, string mark)
        => before.Symbol == after.Symbol
           && before.Symbol is '*' or '_'
           && (mark.Length == 2 ? before.Count >= 2 && after.Count >= 2 : before.Count % 2 == 1 && after.Count % 2 == 1);

    /// <summary>Знаки <c>*</c> или <c>_</c> подряд от <paramref name="at"/> в сторону <paramref name="step"/> до <paramref name="limit"/>.</summary>
    private static (char Symbol, int Count) Run(TextDocument document, int at, int step, int limit)
    {
        if (at < 0 || at >= document.TextLength || at == limit || document.GetCharAt(at) is not ('*' or '_'))
        {
            return ('\0', 0);
        }

        var symbol = document.GetCharAt(at);
        var count = 0;
        for (var current = at; current >= 0 && current < document.TextLength && current != limit && document.GetCharAt(current) == symbol; current += step)
        {
            count++;
        }

        return (symbol, count);
    }

    /// <summary>Слово под курсором — буквы и цифры с обеих сторон от него; курсор вне слова — <c>null</c>.</summary>
    private static (int Start, int End)? Word(TextDocument document, int caret)
    {
        var start = caret;
        while (start > 0 && char.IsLetterOrDigit(document.GetCharAt(start - 1)))
        {
            start--;
        }

        var end = caret;
        while (end < document.TextLength && char.IsLetterOrDigit(document.GetCharAt(end)))
        {
            end++;
        }

        return end > start ? (start, end) : null;
    }

    /// <summary>
    /// Куски выделения по строкам: без маркеров цитаты, пункта с флажком и заголовка в начале строки и без
    /// пробелов по краям; пустые строки пропускаются.
    /// </summary>
    private static List<(int Start, int End)> Segments(TextDocument document, int from, int to)
    {
        var segments = new List<(int Start, int End)>();
        for (var number = document.GetLineByOffset(from).LineNumber; number <= document.GetLineByOffset(to).LineNumber; number++)
        {
            var line = document.GetLineByNumber(number);
            var start = Math.Max(from, line.Offset + LinePrefix().Match(document.GetText(line)).Length);
            var end = Math.Min(to, line.EndOffset);
            while (start < end && char.IsWhiteSpace(document.GetCharAt(start)))
            {
                start++;
            }

            while (end > start && char.IsWhiteSpace(document.GetCharAt(end - 1)))
            {
                end--;
            }

            if (end > start)
            {
                segments.Add((start, end));
            }
        }

        return segments;
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

    /// <summary>
    /// Вставка с форматированием: HTML из данных — Markdown на месте выделения, без HTML — текст, как
    /// обычной вставкой; одним шагом отмены.
    /// </summary>
    public static void PasteFormatted(TextArea area, IDataObject data)
    {
        var text = data.GetDataPresent(DataFormats.Html) && data.GetData(DataFormats.Html) is string { Length: > 0 } html
            ? HtmlMarkdown.Convert(html)
            : data.GetData(DataFormats.UnicodeText) as string;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var document = area.Document;
        using (document.RunUpdate())
        {
            area.Selection.ReplaceSelectionWithText(TextUtilities.NormalizeNewLines(text, TextUtilities.GetNewLineFromDocument(document, area.Caret.Line)));
        }
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

    /// <summary>Начало строки, которое знаки не обрамляют: маркеры цитаты, маркер пункта с флажком задачи, знаки заголовка.</summary>
    [GeneratedRegex(@"^(?:[ \t]*>[ \t]?)*[ \t]*(?:(?:[-*+]|\d{1,9}[.)])[ \t]+(?:\[[ xX]\][ \t]+)?|#{1,6}[ \t]+)?")]
    private static partial Regex LinePrefix();

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
