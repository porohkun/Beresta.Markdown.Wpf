namespace MarkdigWpfViewer;

using Extensions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

internal sealed class ModelBuilder
{
    public static readonly MarkdownPipeline DefaultPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly string[] Bullets = ["•", "◦", "▪"];

    private readonly string _source;
    private readonly List<int> _lineStarts;
    private readonly IReadOnlyList<MdExtension> _extensions;

    private ModelBuilder(string source, IReadOnlyList<MdExtension> extensions)
    {
        _source = source;
        _lineStarts = FindLineStarts(source);
        _extensions = extensions;
    }

    public static IReadOnlyList<BlockVm> Parse(
        string markdown,
        MarkdownPipeline? pipeline = null,
        IReadOnlyList<MdExtension>? extensions = null)
    {
        markdown ??= string.Empty;

        var document = Markdown.Parse(markdown, pipeline ?? DefaultPipeline);
        return new ModelBuilder(markdown, extensions ?? []).ConvertBlocks(document, listDepth: 0);
    }

    internal List<BlockVm> ConvertBlocks(
        ContainerBlock container,
        int listDepth)
    {
        var blocks = new List<BlockVm>();
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
                AddBlankLines(blocks, previous, start);
            }

            blocks.AddRange(converted);
            previousEnd = end;
        }

        return blocks;
    }

    internal IReadOnlyList<InlineVm> ConvertInlines(ContainerInline? container)
    {
        var result = new List<InlineVm>();
        WalkInline(container?.FirstChild, new(), result);
        return result;
    }

    internal MdSourceSpan GetSpan(MarkdownObject node)
    {
        if (node.Span.IsEmpty || node.Span.Start < 0)
        {
            return default;
        }

        var start = node.Span.Start;
        var end = Math.Min(node.Span.End, _source.Length - 1);
        var (startLine, endLine) = node is Block block
            ? GetLines(block)
            : (GetLineAt(start), GetLineAt(end));

        return new(start, end - start + 1, startLine, endLine);
    }

    internal string GetSource(MarkdownObject node)
    {
        return GetSourceText(node.Span);
    }

    private IReadOnlyList<BlockVm> ConvertBlock(
        Block block,
        int listDepth)
    {
        foreach (var extension in _extensions)
        {
            if (extension.ConvertBlock(block, new MdConvertContext(this, listDepth)) is { } custom)
            {
                return [Stamp(custom, block)];
            }
        }

        switch (block)
        {
            case LinkReferenceDefinitionGroup or LinkReferenceDefinition:
                return [];

            case ParagraphBlock paragraph:
                return [Stamp(new ParagraphVm(ConvertInlines(paragraph.Inline)), block)];

            case HeadingBlock heading:
                return [Stamp(new HeadingVm(heading.Level, ConvertInlines(heading.Inline)), block)];

            case ThematicBreakBlock:
                return [Stamp(new ThematicBreakVm(), block)];

            case CodeBlock code:
                return [Stamp(ConvertCodeBlock(code), block)];

            case QuoteBlock quote:
                return [Stamp(new QuoteVm(ConvertBlocks(quote, listDepth)), block)];

            case ListBlock list:
                return [Stamp(ConvertList(list, listDepth), block)];

            case Table table:
                return [Stamp(ConvertTable(table), block)];

            case ContainerBlock container:
                return ConvertBlocks(container, listDepth);

            case LeafBlock { Inline: not null } leaf:
                return [Stamp(new ParagraphVm(ConvertInlines(leaf.Inline)), block)];

            case LeafBlock { Lines.Count: > 0 } leaf:
                return [Stamp(new ParagraphVm(GetRawLines(leaf)), block)];

            default:
                return [];
        }
    }

    private T Stamp<T>(T node, MarkdownObject source)
        where T : NodeVm
    {
        if (!node.Source.IsEmpty)
        {
            return node;
        }

        NodeVm unstamped = node;
        return (T)(unstamped with { Source = GetSpan(source) });
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

        var items = new List<(string Marker, List<BlockVm> Blocks, ListItemBlock Item)>();
        var previousEnd = 0;

        foreach (var child in list)
        {
            if (child is not ListItemBlock item)
            {
                continue;
            }

            var (itemStart, itemEnd) = GetLines(item);
            if (items.Count > 0)
            {
                AddBlankLines(items[^1].Blocks, previousEnd, itemStart);
            }

            var bullet = Bullets[listDepth % Bullets.Length];
            var marker = ordered ? $"{start + items.Count}{list.OrderedDelimiter}" : bullet;
            items.Add((marker, ConvertBlocks(item, listDepth + 1), item));
            previousEnd = itemEnd;
        }

        var converted = items
            .Select(entry => Stamp(new ListItemVm(entry.Marker, entry.Blocks), entry.Item))
            .ToList();

        return new(ordered, start, listDepth, converted);
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
                    cells.Add(Stamp(new TableCellVm(GetCellInlines(cell)), cell));
                }
            }

            maxColumns = Math.Max(maxColumns, cells.Count);

            rows.Add(Stamp(new TableRowVm(IsHeader: r == 0, Cells: cells), row));
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

    private void WalkInline(Inline? current, InlineState state, List<InlineVm> output)
    {
        for (var node = current; node is not null; node = node.NextSibling)
        {
            if (TryConvertByExtension(node, output))
            {
                continue;
            }

            switch (node)
            {
                case LiteralInline literal:
                    AddText(literal.Content.ToString(), state, output, node);
                    break;

                case LineBreakInline { IsHard: true }:
                    output.Add(Stamp(new LineBreakVm(), node));
                    break;

                case LineBreakInline:
                    AddText(" ", state, output, node);
                    break;

                case CodeInline code:
                    AddText(code.Content ?? string.Empty, state with { Code = true }, output, node);
                    break;

                case AutolinkInline autolink:
                    AddLink(autolink.Url, null, state, output, node);
                    break;

                case HtmlInline html:
                    AddText(html.Tag, state, output, node);
                    break;

                case HtmlEntityInline entity:
                    AddText(entity.Transcoded.ToString(), state, output, node);
                    break;

                case LinkInline { IsImage: false } link:
                    AddLink(link.Url, link.FirstChild, state, output, node);
                    break;

                case EmphasisInline emphasis:
                    WalkInline(emphasis.FirstChild, ApplyEmphasis(state, emphasis), output);
                    break;

                case DelimiterInline delimiter:
                    AddText(delimiter.ToLiteral(), state, output, node);
                    WalkInline(delimiter.FirstChild, state, output);
                    break;

                case ContainerInline container:
                    WalkInline(container.FirstChild, state, output);
                    break;

                default:
                    AddText(GetSourceText(node.Span), state, output, node);
                    break;
            }
        }
    }

    private bool TryConvertByExtension(Inline node, List<InlineVm> output)
    {
        foreach (var extension in _extensions)
        {
            if (extension.ConvertInline(node, new MdConvertContext(this, listDepth: 0)) is { } custom)
            {
                output.Add(Stamp(custom, node));
                return true;
            }
        }

        return false;
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

    private void AddLink(string? url, Inline? firstChild, InlineState state, List<InlineVm> output, Inline node)
    {
        var linkedState = state with { Hyperlink = TryCreateUri(url), Href = url };

        if (firstChild is not null)
        {
            WalkInline(firstChild, linkedState, output);
            return;
        }

        if (!string.IsNullOrWhiteSpace(url))
        {
            AddText(url, linkedState, output, node);
        }
    }

    private void AddText(string text, InlineState state, List<InlineVm> output, Inline node)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        output.Add(Stamp(
            new RunVm(
                text,
                state.Bold,
                state.Italic,
                state.Strike,
                state.Code,
                state.Hyperlink,
                state.Href),
            node));
    }

    private void AddBlankLines(List<BlockVm> blocks, int afterLine, int beforeLine)
    {
        for (var line = afterLine + 1; line < beforeLine && line < _lineStarts.Count; line++)
        {
            if (IsBlankLine(line))
            {
                blocks.Add(new BlankLineVm { Source = GetLineSpan(line) });
            }
        }
    }

    private static IReadOnlyList<InlineVm> GetRawLines(LeafBlock leaf)
    {
        var inlines = new List<InlineVm>();

        for (var i = 0; i < leaf.Lines.Count; i++)
        {
            if (i > 0)
            {
                inlines.Add(new LineBreakVm());
            }

            inlines.Add(new RunVm(leaf.Lines.Lines[i].Slice.ToString()));
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

    private MdSourceSpan GetLineSpan(int line)
    {
        var start = _lineStarts[line];
        var end = line + 1 < _lineStarts.Count ? _lineStarts[line + 1] : _source.Length;
        return new(start, end - start, line, line);
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
