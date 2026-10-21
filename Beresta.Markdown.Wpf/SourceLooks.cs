namespace Beresta.Markdown.Wpf;

using Extensions;
using ViewModels;

/// <summary>
/// Как выглядит исходник документа: встроенные элементы — по своему виду, знаки разметки — место
/// элемента за вычетом мест его детей, виджеты расширений — по их описанию. Описания расширений идут
/// последними и рисуются поверх.
/// </summary>
internal static class SourceLooks
{
    private const string Markup = "Source.Markup";

    public static MdSourceLook Describe(
        IReadOnlyList<BlockVm> blocks,
        string source,
        IReadOnlyList<int> lineStarts,
        Func<NodeVm, MdExtension?> ownerOf,
        ExtensionGuard guard)
    {
        var context = new Context(source, lineStarts, ownerOf, guard, new(), new(), new());
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
                case HeadingVm heading:
                    context.Look.Mark(span.Start, span.Length, "Heading" + Math.Clamp(heading.Level, 1, 6));
                    break;
                case QuoteVm:
                    context.Look.MarkLines(span.StartLine, span.EndLine, "Quote");
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

        if (!span.IsEmpty && span.End <= context.Source.Length)
        {
            MarkGaps(span, children, context);
        }
    }

    /// <summary>Кусок текста — по начертанию; обратные кавычки кода по краям — знаки разметки.</summary>
    private static void DescribeRun(RunVm run, MdSourceSpan span, Context context)
    {
        var source = context.Source;
        var (start, end) = (span.Start, span.End);
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

    /// <summary>Дети встроенного элемента с местами в исходнике; лист и чужой элемент — <c>null</c>.</summary>
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
        _ => null
    };

    /// <summary>Текст, его строки и куда складываются описания: встроенные, знаки разметки, расширений.</summary>
    private sealed record Context(
        string Source,
        IReadOnlyList<int> LineStarts,
        Func<NodeVm, MdExtension?> OwnerOf,
        ExtensionGuard Guard,
        MdSourceLook Look,
        MdSourceLook Markup,
        MdSourceLook Extensions);
}
