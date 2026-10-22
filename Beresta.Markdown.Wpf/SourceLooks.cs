namespace Beresta.Markdown.Wpf;

using System.Text.RegularExpressions;
using Extensions;
using Selection;
using ViewModels;

/// <summary>
/// Как выглядит исходник документа: встроенные элементы — по своему виду, знаки разметки — место
/// элемента за вычетом мест его детей, виджеты расширений — по их описанию. Описания расширений идут
/// последними и рисуются поверх.
/// </summary>
internal static partial class SourceLooks
{
    private const string Markup = "Source.Markup";

    public static MdSourceLook Describe(
        IReadOnlyList<BlockVm> blocks,
        string source,
        IReadOnlyList<int> lineStarts,
        Func<NodeVm, MdExtension?> ownerOf,
        ExtensionGuard guard)
    {
        var context = new Context(source, lineStarts, ownerOf, guard, new(source), new(source), new(source), []);
        foreach (var block in blocks)
        {
            Visit(block, context);
        }

        context.Look.Append(context.Markup);
        context.Look.Append(context.Extensions);
        return context.Look;
    }

    private static void Visit(NodeVm node, Context context)
    {
        if (context.OwnerOf(node) is { } owner)
        {
            context.Guard.Call(owner, () => owner.DescribeSource(node, context.Extensions));
        }

        var span = node.Source;
        if (!span.IsEmpty && span.End <= context.Source.Length)
        {
            switch (node)
            {
                case RunVm run:
                    DescribeRun(run, span, context);
                    return;
                case CodeBlockVm code:
                    DescribeCode(code, span, context);
                    return;
                case ThematicBreakVm:
                    context.Markup.Mark(span.Start, span.Length, Markup);
                    return;
                case BlankLineVm:
                    context.Look.MarkLines(span.StartLine, span.EndLine, "BlankLine");
                    return;
                case HeadingVm heading:
                    context.Look.Mark(span.Start, span.Length, "Heading" + Math.Clamp(heading.Level, 1, 6));
                    break;
                case QuoteVm:
                    context.Look.MarkLines(span.StartLine, span.EndLine, "Quote");
                    MarkQuoteIndents(span, context);
                    break;
                case CalloutVm callout:
                    context.Look.MarkLines(span.StartLine, LastLine(span, context), CalloutLook(callout));
                    MarkQuoteIndents(span, context);
                    break;
                case ListItemVm item:
                    MarkItemIndent(item, context);
                    break;
                case TableVm table:
                    MarkTableColumns(table, span, context);
                    break;
            }
        }

        if (ChildrenOf(node) is not { } children)
        {
            return;
        }

        foreach (var child in children)
        {
            Visit(child, context);
        }

        // BYDESIGN: у детей без мест в исходнике знаков разметки не узнать — строка CSV, к примеру, не серая
        if (!span.IsEmpty && span.End <= context.Source.Length && children.Any(child => !child.Source.IsEmpty))
        {
            MarkGaps(span, children, context);
        }

        MarkCalloutKind(node, span, context);
    }

    /// <summary>Вид плашки: <c>Callout.</c> и её вид с большой буквы, как в ключах темы.</summary>
    private static string CalloutLook(CalloutVm callout)
    {
        var kind = callout.Kind.Trim().ToLowerInvariant();
        return "Callout." + (kind.Length == 0 ? "Note" : char.ToUpperInvariant(kind[0]) + kind[1..]);
    }

    /// <summary>
    /// Вид плашки в строке её заголовка — <c>[!note]</c>, слово вида у <c>!!! note</c> и <c>:::note</c> —
    /// видом плашки поверх знаков разметки.
    /// </summary>
    private static void MarkCalloutKind(NodeVm node, MdSourceSpan span, Context context)
    {
        if (node is not CalloutVm callout || span.IsEmpty || span.End > context.Source.Length)
        {
            return;
        }

        var end = LineEnd(context, span.StartLine);
        if (end > span.Start && CalloutKind().Match(context.Source, span.Start, end - span.Start) is { Success: true } match)
        {
            var kind = match.Groups["kind"];
            context.Markup.Mark(kind.Index, kind.Length, CalloutLook(callout));
        }
    }

    /// <summary>
    /// Столбцы таблицы с вертикальными чертами: в каждой её строке — неэкранированные <c>|</c> после
    /// ячеек, без черты в начале. Таблица без строки разделителей вторым рядом — сетка — не отмечается.
    /// </summary>
    private static void MarkTableColumns(TableVm table, MdSourceSpan span, Context context)
    {
        if (span.StartLine + 1 > span.EndLine || span.EndLine >= context.LineStarts.Count)
        {
            return;
        }

        var column = span.Start - context.LineStarts[span.StartLine];
        var rows = new List<(int Line, IReadOnlyList<int> Separators)>();
        for (var line = span.StartLine; line <= span.EndLine; line++)
        {
            var end = LineEnd(context, line);
            var first = Math.Min(context.LineStarts[line] + column, end);
            while (first < end && context.Source[first] is ' ' or '\t')
            {
                first++;
            }

            if (line == span.StartLine + 1 && !Delimiters().IsMatch(context.Source.AsSpan(first, end - first)))
            {
                return;
            }

            var separators = new List<int>();
            for (var at = first; at < end; at++)
            {
                if (context.Source[at] == '\\')
                {
                    at++;
                }
                else if (context.Source[at] == '|' && at != first)
                {
                    separators.Add(at);
                }
            }

            rows.Add((line, separators));
        }

        context.Look.MarkColumns(rows, table.Alignments);
    }

    /// <summary>
    /// Кусок текста — по начертанию и своему виду; рамка куска и обратные кавычки кода по краям — знаки
    /// разметки.
    /// </summary>
    private static void DescribeRun(RunVm run, MdSourceSpan span, Context context)
    {
        var source = context.Source;
        var (start, end) = run.Markup is { } frame ? Frame(frame, span, context) : (span.Start, span.End);
        if (run.Code)
        {
            var open = 0;
            while (start + open < end && source[start + open] == '`')
            {
                open++;
            }

            var close = 0;
            while (end - close - 1 >= start + open && source[end - close - 1] == '`')
            {
                close++;
            }

            if (open > 0 && close > 0)
            {
                context.Markup.Mark(start, open, Markup);
                context.Markup.Mark(end - close, close, Markup);
                (start, end) = (start + open, end - close);
            }
        }

        var length = end - start;
        if (run.Bold)
        {
            context.Look.Mark(start, length, "Bold");
        }

        if (run.Italic)
        {
            context.Look.Mark(start, length, "Italic");
        }

        if (run.Strike)
        {
            context.Look.Mark(start, length, "Strike");
        }

        if (run.Code)
        {
            context.Look.Mark(start, length, "InlineCode");
        }

        if (run.Href is not null)
        {
            context.Look.Mark(start, length, "Link");
        }

        if (run.StyleName is { } style)
        {
            context.Look.Mark(start, length, style);
        }
    }

    /// <summary>
    /// Текст куска в рамке: открывающий знак, с которого исходник начинается, и закрывающий — в его конце
    /// или первый за текстом — знаки разметки. Куски одного узла — замена CriticMarkup — идут по его
    /// исходнику друг за другом.
    /// </summary>
    private static (int Start, int End) Frame(MarkupFrame frame, MdSourceSpan span, Context context)
    {
        var source = context.Source.AsSpan();
        var start = context.Framed.GetValueOrDefault(span, span.Start);
        var end = span.End;
        if (frame.Open.Length > 0 && source[start..end].StartsWith(frame.Open, StringComparison.Ordinal))
        {
            context.Markup.Mark(start, frame.Open.Length, Markup);
            start += frame.Open.Length;
        }

        var next = end;
        if (frame.Close.Length > 0)
        {
            var close = source[start..end].EndsWith(frame.Close, StringComparison.Ordinal)
                ? end - frame.Close.Length
                : source[start..end].IndexOf(frame.Close, StringComparison.Ordinal) is var at and >= 0 ? start + at : -1;
            if (close >= 0)
            {
                context.Markup.Mark(close, frame.Close.Length, Markup);
                (end, next) = (close, close + frame.Close.Length);
            }
        }

        context.Framed[span] = next;
        return (start, end);
    }

    /// <summary>Маркер пункта — от начала строки до его текста, у задачи — и до текста после флажка.</summary>
    private static void MarkItemIndent(ListItemVm item, Context context)
    {
        if (item.Blocks is not [{ Source: { IsEmpty: false } first } block, ..] || first.StartLine >= context.LineStarts.Count)
        {
            return;
        }

        var end = first.Start;
        if (block is ParagraphVm { Inlines: [TaskVm { Source: { IsEmpty: false } task }, ..] })
        {
            end = task.End;
            while (end < context.Source.Length && context.Source[end] == ' ')
            {
                end++;
            }
        }

        context.Look.MarkIndent(first.StartLine, end - context.LineStarts[first.StartLine]);
    }

    /// <summary>Маркеры строк цитаты: <c>&gt;</c> с пробелом, у вложенной цитаты — все подряд.</summary>
    private static void MarkQuoteIndents(MdSourceSpan span, Context context)
    {
        for (var line = span.StartLine; line <= span.EndLine && line < context.LineStarts.Count; line++)
        {
            var start = context.LineStarts[line];
            var end = LineEnd(context, line);
            var at = start;
            while (true)
            {
                var mark = at;
                while (mark < end && mark - at < 3 && context.Source[mark] == ' ')
                {
                    mark++;
                }

                if (mark >= end || context.Source[mark] != '>')
                {
                    break;
                }

                at = mark + 1 < end && context.Source[mark + 1] == ' ' ? mark + 2 : mark + 1;
            }

            context.Look.MarkIndent(line, at - start);
        }
    }

    /// <summary>Строки блока кода — фоном, сам код — шрифтом и цветом кода, ограждения — знаками разметки.</summary>
    private static void DescribeCode(CodeBlockVm code, MdSourceSpan span, Context context)
    {
        context.Look.MarkLines(span.StartLine, span.EndLine, "CodeBlock");
        var first = code.CodeFirstLine;
        var last = Math.Min(first + (code.Code.Length == 0 ? 0 : code.Code.Split('\n').Length) - 1, span.EndLine);
        for (var line = span.StartLine; line <= span.EndLine && line < context.LineStarts.Count; line++)
        {
            var start = context.LineStarts[line];
            var length = LineEnd(context, line) - start;
            if (line >= first && line <= last)
            {
                context.Look.Mark(start, length, "CodeBlock");
            }
            else
            {
                context.Markup.Mark(start, length, Markup);
            }
        }

        var lines = code.Code.Split('\n');
        var offset = 0;
        foreach (var run in code.Runs.OfType<RunVm>())
        {
            if (run.StyleName is { } look)
            {
                MarkCode(code, lines, offset, run.Text.Length, look, context);
            }

            offset += run.Text.Length;
        }
    }

    /// <summary>Кусок подсвеченного кода — по строкам кода, на их местах в строках исходника.</summary>
    private static void MarkCode(CodeBlockVm code, string[] lines, int start, int length, string look, Context context)
    {
        var lineStart = 0;
        for (var index = 0; index < lines.Length && lineStart < start + length; index++)
        {
            var lineEnd = lineStart + lines[index].Length;
            var from = Math.Max(start, lineStart);
            var to = Math.Min(start + length, lineEnd);
            var line = code.CodeFirstLine + index;
            if (to > from && line < context.LineStarts.Count)
            {
                var codeStart = LineEnd(context, line) - lines[index].Length;
                if (codeStart >= context.LineStarts[line] && context.Source.AsSpan(codeStart, lines[index].Length).SequenceEqual(lines[index]))
                {
                    context.Look.Mark(codeStart + (from - lineStart), to - from, look);
                }
            }

            lineStart = lineEnd + 1;
        }
    }

    /// <summary>Знаки разметки — место элемента за вычетом мест его детей; одни пробелы знаками не считаются.</summary>
    private static void MarkGaps(MdSourceSpan span, IEnumerable<NodeVm> children, Context context)
    {
        var cursor = span.Start;
        foreach (var child in children
            .Select(child => child.Source)
            .Where(source => !source.IsEmpty)
            .OrderBy(source => source.Start))
        {
            Gap(cursor, Math.Min(child.Start, span.End), context);
            cursor = Math.Max(cursor, child.End);
        }

        Gap(cursor, span.End, context);
    }

    private static void Gap(int start, int end, Context context)
    {
        if (end > start && !context.Source.AsSpan(start, end - start).IsWhiteSpace())
        {
            context.Markup.Mark(start, end - start, Markup);
        }
    }

    /// <summary>Последняя строка места — с закрывающим ограждением, которого нет в строках контейнера.</summary>
    private static int LastLine(MdSourceSpan span, Context context)
    {
        var line = span.EndLine;
        while (line + 1 < context.LineStarts.Count && context.LineStarts[line + 1] < span.End)
        {
            line++;
        }

        return line;
    }

    /// <summary>Конец строки исходника с нуля — без перевода строки.</summary>
    private static int LineEnd(Context context, int line)
    {
        var start = context.LineStarts[line];
        var end = line + 1 < context.LineStarts.Count ? context.LineStarts[line + 1] : context.Source.Length;
        while (end > start && context.Source[end - 1] is '\r' or '\n')
        {
            end--;
        }

        return end;
    }

    /// <summary>
    /// Дети элемента с местами в исходнике: у встроенного — свои, у своей модели — вложенное, которое она
    /// пишет в текстовую карту; лист — <c>null</c>.
    /// </summary>
    private static IEnumerable<NodeVm>? ChildrenOf(NodeVm node) => node switch
    {
        ParagraphVm paragraph => paragraph.Inlines,
        HeadingVm heading => heading.Inlines,
        FigureCaptionVm caption => caption.Inlines,
        TableCellVm cell => cell.Inlines,
        QuoteVm quote => quote.Blocks,
        ListItemVm item => item.Blocks,
        FigureVm figure => figure.Blocks,
        FooterVm footer => footer.Blocks,
        AlignedVm aligned => aligned.Blocks,
        FootnoteVm footnote => footnote.Blocks,
        ListVm list => list.Items,
        TableVm table => table.Rows,
        TableRowVm row => row.Cells,
        DefinitionListVm definitions => definitions.Items,
        CalloutVm callout => [.. callout.Title, .. callout.Blocks],
        DetailsVm details => [.. details.Summary, .. details.Blocks],
        DefinitionItemVm item => [.. item.Terms, .. item.Blocks],
        IMdTextSource source => Nested(node, source),
        _ => null
    };

    /// <summary>Инлайны и блоки, которые своя модель пишет в карту; ни у одного нет места в исходнике — <c>null</c>.</summary>
    private static List<NodeVm>? Nested(NodeVm node, IMdTextSource source)
    {
        var nested = new List<NodeVm>();
        var writer = new MdTextWriter(_ => { }, inlines => nested.AddRange(inlines), blocks => nested.AddRange(blocks));

        // BYDESIGN: сбой виджета молчит — его уже отдала в RenderFailed сборка текстовой карты
        return ExtensionGuard.Silent.CallWidget(node, () => source.WriteText(writer)) && nested.Any(child => !child.Source.IsEmpty)
            ? nested
            : null;
    }

    [GeneratedRegex(@"^\|?\s*:?-+:?\s*(\|\s*:?-+:?\s*)*\|?\s*$")]
    private static partial Regex Delimiters();

    [GeneratedRegex(@"(?<kind>\[![A-Za-z][\w-]*\][+-]?)|(?:!!!|\?\?\?\+?)[ \t]+(?<kind>[A-Za-z][\w-]*)|:{3,}[ \t]*(?<kind>[A-Za-z][\w-]*)")]
    private static partial Regex CalloutKind();

    /// <summary>
    /// Текст, его строки и куда складываются описания: встроенные, знаки разметки, расширений; где
    /// начинается следующий кусок в рамке у места, которое делят куски одного узла.
    /// </summary>
    private sealed record Context(
        string Source,
        IReadOnlyList<int> LineStarts,
        Func<NodeVm, MdExtension?> OwnerOf,
        ExtensionGuard Guard,
        MdSourceLook Look,
        MdSourceLook Markup,
        MdSourceLook Extensions,
        Dictionary<MdSourceSpan, int> Framed);
}
