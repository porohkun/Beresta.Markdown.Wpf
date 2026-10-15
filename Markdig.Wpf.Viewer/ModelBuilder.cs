namespace MarkdigWpfViewer;

using Abstractions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

internal sealed class ModelBuilder
{
    private static readonly MarkdownPipeline DefaultPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly string[] Bullets = ["•", "◦", "▪"];

    private readonly string _source;
    private readonly List<int> _lineStarts;

    private ModelBuilder(string source)
    {
        _source = source;
        _lineStarts = FindLineStarts(source);
    }

    public static IReadOnlyList<IMdBlockVm> Parse(
        string markdown,
        MarkdownPipeline? pipeline = null)
    {
        markdown ??= string.Empty;

        var document = Markdown.Parse(markdown, pipeline ?? DefaultPipeline);
        return new ModelBuilder(markdown).ConvertBlocks(document, listDepth: 0);
    }

    private List<IMdBlockVm> ConvertBlocks(
        ContainerBlock container,
        int listDepth)
    {
        var blocks = new List<IMdBlockVm>();
        int? previousEnd = null;

        foreach (var child in container)
        {
            var converted = ConvertBlock(child, listDepth);
            if (converted.Count == 0)
            {
                continue;
            }

            var (start, end) = GetLines(child);
            if (previousEnd is { } previous)
            {
                AddBlankLines(blocks, CountBlankLines(previous, start));
            }

            blocks.AddRange(converted);
            previousEnd = end;
        }

        return blocks;
    }

    private IReadOnlyList<IMdBlockVm> ConvertBlock(
        Block block,
        int listDepth)
    {
        return block switch
        {
            ParagraphBlock paragraph => [new ParagraphVm(ConvertInlines(paragraph.Inline))],
            HeadingBlock heading => [new HeadingVm(heading.Level, ConvertInlines(heading.Inline))],
            ThematicBreakBlock => [new ThematicBreakVm()],
            CodeBlock code => [ConvertCodeBlock(code)],
            QuoteBlock quote => [new QuoteVm(ConvertBlocks(quote, listDepth))],
            ListBlock list => [ConvertList(list, listDepth)],
            Table table => [ConvertTable(table)],
            LinkReferenceDefinitionGroup or LinkReferenceDefinition => [],
            ContainerBlock container => ConvertBlocks(container, listDepth),
            LeafBlock { Inline: not null } leaf => [new ParagraphVm(ConvertInlines(leaf.Inline))],
            LeafBlock { Lines.Count: > 0 } leaf => [new ParagraphVm(GetRawLines(leaf))],
            _ => []
        };
    }

    private static CodeBlockVm ConvertCodeBlock(CodeBlock block)
    {
        var info = (block as FencedCodeBlock)?.Info;
        return new(info, ParseLanguage(info), GetLeafText(block));
    }

    private ListVm ConvertList(ListBlock list, int listDepth)
    {
        var ordered = list.IsOrdered;
        var start = ordered && int.TryParse(list.OrderedStart, out var number) ? number : 1;

        var markers = new List<string>();
        var contents = new List<List<IMdBlockVm>>();
        var previousEnd = 0;

        foreach (var child in list)
        {
            if (child is not ListItemBlock item)
            {
                continue;
            }

            var (itemStart, itemEnd) = GetLines(item);
            if (contents.Count > 0)
            {
                AddBlankLines(contents[^1], CountBlankLines(previousEnd, itemStart));
            }

            var bullet = Bullets[listDepth % Bullets.Length];
            markers.Add(ordered ? $"{start + markers.Count}{list.OrderedDelimiter}" : bullet);
            contents.Add(ConvertBlocks(item, listDepth + 1));
            previousEnd = itemEnd;
        }

        var items = markers
            .Select((marker, index) => new ListItemVm(marker, contents[index]))
            .ToList();

        return new(ordered, start, listDepth, items);
    }

    private TableVm ConvertTable(Table table)
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

    private IReadOnlyList<InlineVm> GetCellInlines(TableCell cell)
    {
        var paragraph = cell.Descendants<ParagraphBlock>().FirstOrDefault();
        return paragraph?.Inline is { } inline
            ? ConvertInlines(inline)
            : [];
    }

    private IReadOnlyList<InlineVm> ConvertInlines(ContainerInline? container)
    {
        var result = new List<InlineVm>();
        WalkInline(container?.FirstChild, new(), result);
        return result;
    }

    private void WalkInline(Inline? current, InlineState state, List<InlineVm> output)
    {
        for (var node = current; node is not null; node = node.NextSibling)
        {
            switch (node)
            {
                case LiteralInline literal:
                    AddText(literal.Content.ToString(), state, output);
                    break;

                case LineBreakInline { IsHard: true }:
                    output.Add(new(
                        string.Empty,
                        state.Bold,
                        state.Italic,
                        state.Strike,
                        state.Code,
                        state.Hyperlink,
                        LineBreak: true,
                        Href: state.Href));
                    break;

                case LineBreakInline:
                    AddText(" ", state, output);
                    break;

                case CodeInline code:
                    AddText(code.Content ?? string.Empty, state with { Code = true }, output);
                    break;

                case AutolinkInline autolink:
                    AddLink(autolink.Url, autolink, state, output);
                    break;

                case HtmlInline html:
                    AddText(html.Tag, state, output);
                    break;

                case HtmlEntityInline entity:
                    AddText(entity.Transcoded.ToString(), state, output);
                    break;

                case LinkInline { IsImage: false } link:
                    AddLink(link.Url, link.FirstChild, state, output);
                    break;

                case EmphasisInline emphasis:
                    WalkInline(emphasis.FirstChild, ApplyEmphasis(state, emphasis), output);
                    break;

                case DelimiterInline delimiter:
                    AddText(delimiter.ToLiteral(), state, output);
                    WalkInline(delimiter.FirstChild, state, output);
                    break;

                case ContainerInline container:
                    WalkInline(container.FirstChild, state, output);
                    break;

                default:
                    AddText(GetSourceText(node.Span), state, output);
                    break;
            }
        }
    }

    private static InlineState ApplyEmphasis(InlineState state, EmphasisInline emphasis)
    {
        if (emphasis.DelimiterChar is '*' or '_')
        {
            if (emphasis.DelimiterCount >= 2)
            {
                state = state with { Bold = true };
            }

            if ((emphasis.DelimiterCount & 1) == 1)
            {
                state = state with { Italic = true };
            }
        }
        else if (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2)
        {
            state = state with { Strike = true };
        }

        return state;
    }

    private void AddLink(string? url, Inline? firstChild, InlineState state, List<InlineVm> output)
    {
        var linkedState = state with { Hyperlink = TryCreateUri(url), Href = url };

        if (firstChild is not null)
        {
            WalkInline(firstChild, linkedState, output);
            return;
        }

        if (!string.IsNullOrWhiteSpace(url))
        {
            AddText(url, linkedState, output);
        }
    }

    private static void AddText(string text, InlineState state, List<InlineVm> output)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        output.Add(new(
            text,
            state.Bold,
            state.Italic,
            state.Strike,
            state.Code,
            state.Hyperlink,
            Href: state.Href));
    }

    private static void AddBlankLines(List<IMdBlockVm> blocks, int count)
    {
        for (var i = 0; i < count; i++)
        {
            blocks.Add(new BlankLineVm());
        }
    }

    private static IReadOnlyList<InlineVm> GetRawLines(LeafBlock leaf)
    {
        var inlines = new List<InlineVm>();

        for (var i = 0; i < leaf.Lines.Count; i++)
        {
            if (i > 0)
            {
                inlines.Add(new(string.Empty, LineBreak: true));
            }

            inlines.Add(new(leaf.Lines.Lines[i].Slice.ToString()));
        }

        return inlines;
    }

    private (int Start, int End) GetLines(Block block)
    {
        var end = block is ContainerBlock { LastChild: { } last }
            ? GetLines(last).End
            : GetLineAt(block.Span.End);

        return (block.Line, Math.Max(block.Line, end));
    }

    private int CountBlankLines(int afterLine, int beforeLine)
    {
        var count = 0;

        for (var line = afterLine + 1; line < beforeLine && line < _lineStarts.Count; line++)
        {
            if (IsBlankLine(line))
            {
                count++;
            }
        }

        return count;
    }

    private bool IsBlankLine(int line)
    {
        var end = line + 1 < _lineStarts.Count ? _lineStarts[line + 1] : _source.Length;

        for (var i = _lineStarts[line]; i < end; i++)
        {
            if (_source[i] is not (' ' or '\t' or '>' or '\r' or '\n'))
            {
                return false;
            }
        }

        return true;
    }

    private int GetLineAt(int position)
    {
        if (position < 0)
        {
            return 0;
        }

        var index = _lineStarts.BinarySearch(position);
        return index >= 0 ? index : ~index - 1;
    }

    private string GetSourceText(SourceSpan span)
    {
        return span.Start >= 0 && span.Length > 0 && span.End < _source.Length
            ? _source.Substring(span.Start, span.Length)
            : string.Empty;
    }

    private static List<int> FindLineStarts(string text)
    {
        var starts = new List<int> { 0 };

        for (var i = 0; i < text.Length; i++)
        {
            var lineEnd = text[i] == '\n'
                          || (text[i] == '\r' && (i + 1 == text.Length || text[i + 1] != '\n'));
            if (lineEnd)
            {
                starts.Add(i + 1);
            }
        }

        return starts;
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

    private readonly record struct InlineState(
        bool Bold = false,
        bool Italic = false,
        bool Strike = false,
        bool Code = false,
        Uri? Hyperlink = null,
        string? Href = null);
}
