namespace MarkdigWpfViewer;

using System.Text;
using Abstractions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

internal static class ModelBuilder
{
    private static readonly MarkdownPipeline DefaultPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static IReadOnlyList<IMdBlockVm> Parse(
        string markdown,
        MarkdownPipeline? pipeline = null)
    {
        pipeline ??= DefaultPipeline;
        markdown ??= string.Empty;

        var result = new List<IMdBlockVm>();

        foreach (var segment in SplitIntoSegments(markdown))
        {
            if (segment.IsBlank)
            {
                result.Add(new BlankLineVm());
                continue;
            }

            var document = Markdown.Parse(segment.Text!, pipeline);
            result.AddRange(ConvertBlocks(document, depth: 0));
        }

        return result;
    }

    private static IReadOnlyList<IMdBlockVm> ConvertBlocks(
        ContainerBlock container,
        int depth)
    {
        var blocks = new List<IMdBlockVm>();

        foreach (var obj in container)
        {
            if (obj == null)
                continue;

            var converted = ConvertBlock(obj, depth);
            if (converted is not null)
            {
                blocks.Add(converted);
            }
        }

        return blocks;
    }

    private static IMdBlockVm? ConvertBlock(
        Block block,
        int depth)
    {
        return block switch
        {
            ParagraphBlock paragraph => new ParagraphVm(ConvertInlines(paragraph.Inline)),
            HeadingBlock heading => new HeadingVm(heading.Level, ConvertInlines(heading.Inline)),
            ThematicBreakBlock => new ThematicBreakVm(),
            FencedCodeBlock fenced => ConvertCodeBlock(fenced),
            CodeBlock code => ConvertCodeBlock(code),
            QuoteBlock quote => new QuoteVm(ConvertBlocks(quote, depth + 1)),
            ListBlock list => ConvertList(list, depth),
            Table table => ConvertTable(table),
            _ => null
        };
    }

    private static CodeBlockVm ConvertCodeBlock(CodeBlock block)
    {
        var code = GetLeafText(block);
        var info = TryGetStringProperty(block, "InfoString")
                   ?? TryGetStringProperty(block, "Info");

        var language = ParseLanguage(info);
        return new(info, language, code);
    }

    private static ListVm ConvertList(ListBlock list, int depth)
    {
        var ordered = TryGetBoolProperty(list, "IsOrdered")
                      ?? TryGetBoolProperty(list, "Ordered")
                      ?? false;

        var start = TryGetIntProperty(list, "StartingNumber")
                    ?? TryGetIntProperty(list, "Start")
                    ?? 1;

        var items = new List<ListItemVm>();
        var index = 0;

        foreach (var child in list)
        {
            if (child is not ListItemBlock item)
            {
                continue;
            }

            var blocks = ConvertBlocks(item, depth + 1);
            var markerText = ordered ? $"{start + index}." : "•";
            items.Add(new(markerText, blocks));
            index++;
        }

        return new(ordered, start, depth, items);
    }

    private static TableVm ConvertTable(Table table)
    {
        var rows = new List<TableRowVm>();
        var maxColumns = table.ColumnDefinitions.Count;

        for (var r = 0; r < table.Count; r++)
        {
            if (table[r] is not TableRow row)
            {
                continue;
            }

            var cells = new List<TableCellVm>();
            for (var c = 0; c < row.Count; c++)
            {
                if (row[c] is TableCell cell)
                {
                    cells.Add(new(GetCellInlines(cell)));
                }
            }

            maxColumns = Math.Max(maxColumns, cells.Count);

            rows.Add(new(IsHeader: r == 0, Cells: cells));
        }

        foreach (var row in rows)
        {
            while (row.Cells.Count < maxColumns)
            {
                if (row.Cells is List<TableCellVm> list)
                {
                    list.Add(new([]));
                }
                else
                {
                    break;
                }
            }
        }

        return new(maxColumns, rows);
    }

    private static IReadOnlyList<InlineVm> GetCellInlines(TableCell cell)
    {
        var paragraph = cell.Descendants<ParagraphBlock>().FirstOrDefault();
        return paragraph?.Inline is { } inline
            ? ConvertInlines(inline)
            : [];
    }

    private static IReadOnlyList<InlineVm> ConvertInlines(ContainerInline? container)
    {
        var result = new List<InlineVm>();

        if (container?.FirstChild is not null)
        {
            WalkInline(container.FirstChild, new(), result);
        }

        return result;
    }

    private static void WalkInline(Inline? current, InlineState state, List<InlineVm> output)
    {
        for (var node = current; node is not null; node = node.NextSibling)
        {
            switch (node)
            {
                case LiteralInline literal:
                {
                    var text = literal.Content.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        output.Add(new(
                            text,
                            state.Bold,
                            state.Italic,
                            state.Strike,
                            state.Code,
                            state.Hyperlink,
                            Href: state.Href));
                    }

                    break;
                }

                case LineBreakInline lineBreak:
                {
                    if (lineBreak.IsHard)
                    {
                        output.Add(new(
                            string.Empty,
                            state.Bold,
                            state.Italic,
                            state.Strike,
                            state.Code,
                            state.Hyperlink,
                            LineBreak: true,
                            Href: state.Href));
                    }
                    else
                    {
                        output.Add(new(
                            " ",
                            state.Bold,
                            state.Italic,
                            state.Strike,
                            state.Code,
                            state.Hyperlink,
                            Href: state.Href));
                    }

                    break;
                }

                case CodeInline code:
                {
                    output.Add(new(
                        code.Content ?? string.Empty,
                        state.Bold,
                        state.Italic,
                        state.Strike,
                        Code: true,
                        Hyperlink: state.Hyperlink,
                        Href: state.Href));
                    break;
                }

                case AutolinkInline autolink:
                {
                    AddLink(autolink.Url, autolink, state, output);
                    break;
                }

                case LinkInline { IsImage: false } link:
                {
                    AddLink(link.Url, link.FirstChild, state, output);
                    break;
                }

                case EmphasisInline emphasis:
                {
                    var childState = state;

                    if (emphasis.DelimiterCount >= 2)
                    {
                        childState = childState with { Bold = true };
                    }

                    if ((emphasis.DelimiterCount & 1) == 1)
                    {
                        childState = childState with { Italic = true };
                    }

                    if (emphasis.FirstChild is not null)
                    {
                        WalkInline(emphasis.FirstChild, childState, output);
                    }

                    break;
                }

                case ContainerInline { FirstChild: not null } container:
                {
                    WalkInline(container.FirstChild, state, output);
                    break;
                }

                case HtmlInline html:
                {
                    var text = html.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        output.Add(new(
                            text,
                            state.Bold,
                            state.Italic,
                            state.Strike,
                            state.Code,
                            state.Hyperlink,
                            Href: state.Href));
                    }

                    break;
                }
            }
        }
    }

    private static void AddLink(string? url, Inline? firstChild, InlineState state, List<InlineVm> output)
    {
        var uri = TryCreateUri(url);
        var linkedState = state with { Hyperlink = uri, Href = url };

        if (firstChild is not null)
        {
            WalkInline(firstChild, linkedState, output);
            return;
        }

        if (!string.IsNullOrWhiteSpace(url))
        {
            output.Add(new(url, state.Bold, state.Italic, state.Strike, state.Code, uri, Href: url));
        }
    }

    private static string? ParseLanguage(string? infoString)
    {
        if (string.IsNullOrWhiteSpace(infoString))
        {
            return null;
        }

        var firstToken = infoString
            .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(firstToken) ? null : firstToken;
    }

    private static string GetLeafText(LeafBlock block)
    {
        return block.Lines.ToString().TrimEnd('\r', '\n') ?? string.Empty;
    }

    private static Uri? TryCreateUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
    }

    private static bool? TryGetBoolProperty(object obj, string name)
        => obj.GetType().GetProperty(name)?.GetValue(obj) as bool?;

    private static int? TryGetIntProperty(object obj, string name)
        => obj.GetType().GetProperty(name)?.GetValue(obj) as int?;

    private static string? TryGetStringProperty(object obj, string name)
        => obj.GetType().GetProperty(name)?.GetValue(obj) as string;

    private static IReadOnlyList<MarkdownSegment> SplitIntoSegments(string markdown)
    {
        var segments = new List<MarkdownSegment>();
        var sb = new StringBuilder();
        var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var inFence = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            var isFence = trimmed.StartsWith("```", StringComparison.Ordinal)
                          || trimmed.StartsWith("~~~", StringComparison.Ordinal);

            if (isFence)
            {
                inFence = !inFence;
            }

            if (!inFence && string.IsNullOrWhiteSpace(line))
            {
                if (sb.Length > 0)
                {
                    segments.Add(new(sb.ToString(), false));
                    sb.Clear();
                }

                segments.Add(new(null, true));
                continue;
            }

            sb.AppendLine(line);
        }

        if (sb.Length > 0)
        {
            segments.Add(new(sb.ToString(), false));
        }

        return segments;
    }

    private readonly record struct InlineState(
        bool Bold = false,
        bool Italic = false,
        bool Strike = false,
        bool Code = false,
        Uri? Hyperlink = null,
        string? Href = null);

    private readonly record struct MarkdownSegment(string? Text, bool IsBlank);
}