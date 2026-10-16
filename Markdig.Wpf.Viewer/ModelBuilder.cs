namespace MarkdigWpfViewer;

using Extensions;
using Identity;
using System.Globalization;
using System.Net;
using System.Text;
using Markdig;
using Markdig.Extensions.Abbreviations;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.DefinitionLists;
using Markdig.Extensions.Figures;
using Markdig.Extensions.Footers;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.SmartyPants;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Extensions.Yaml;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

internal sealed class ModelBuilder
{
    public static readonly MarkdownPipeline DefaultPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    private static readonly string[] Bullets = ["•", "◦", "▪"];

    private readonly string _source;
    private readonly List<int> _lineStarts;
    private readonly IReadOnlyList<MdExtension> _extensions;
    private readonly ModelIdentity _identity;
    private readonly IReadOnlyDictionary<SmartyPantType, string>? _smartyPants;

    private ModelBuilder(
        string source,
        IReadOnlyList<MdExtension> extensions,
        ModelIdentity identity,
        IReadOnlyDictionary<SmartyPantType, string>? smartyPants)
    {
        _source = source;
        _lineStarts = FindLineStarts(source);
        _extensions = extensions;
        _identity = identity;
        _smartyPants = smartyPants;
    }

    /// <summary>
    /// Разбирает документ; с памятью прошлого разбора элементы сохраняют идентификаторы, а
    /// неизменные — вьюмодели.
    /// </summary>
    public static IReadOnlyList<BlockVm> Parse(
        string markdown,
        MarkdownPipeline? pipeline = null,
        IReadOnlyList<MdExtension>? extensions = null,
        ModelIdentity? identity = null)
        => Parse(markdown, pipeline, extensions, identity, out _);

    /// <summary>
    /// Разбирает документ, как <see cref="Parse(string, MarkdownPipeline?, IReadOnlyList{MdExtension}?, ModelIdentity?)"/>,
    /// и отдаёт текст front matter без разбора; нет его — <c>null</c>.
    /// </summary>
    public static IReadOnlyList<BlockVm> Parse(
        string markdown,
        MarkdownPipeline? pipeline,
        IReadOnlyList<MdExtension>? extensions,
        ModelIdentity? identity,
        out string? frontMatter)
    {
        markdown ??= string.Empty;
        identity ??= new();
        pipeline ??= DefaultPipeline;

        var document = Markdown.Parse(markdown, pipeline);
        identity.Match(document, markdown);
        frontMatter = document is [YamlFrontMatterBlock yaml, ..] ? yaml.Lines.ToString() : null;

        var smartyPants = pipeline.Extensions.Find<SmartyPantsExtension>()?.Options.Mapping;
        var blocks = new ModelBuilder(markdown, extensions ?? [], identity, smartyPants).ConvertBlocks(document, listDepth: 0);
        identity.Commit();
        return blocks;
    }

    /// <summary>
    /// Блоки контейнера с отступами на месте пустых строк. Определения сносок, которые Markdig
    /// собирает в конец документа, встают туда, где они написаны. В ячейке сетки-таблицы
    /// <paramref name="gapsAreBlank"/>: строка между её блоками пуста внутри столбца, даже если в
    /// соседних столбцах на ней текст.
    /// </summary>
    internal List<BlockVm> ConvertBlocks(
        ContainerBlock container,
        int listDepth,
        bool gapsAreBlank = false)
    {
        var blocks = new List<BlockVm>();
        int? previousEnd = null;

        var children = container
            .SelectMany(child => child is FootnoteGroup group ? group.AsEnumerable() : new[] { child })
            .OrderBy(child => child.Line);

        foreach (var child in children)
        {
            var converted = ConvertBlock(child, listDepth);
            if (converted.Count == 0)
            {
                continue;
            }

            var (start, end) = GetLines(child);
            if (previousEnd is { } previous)
            {
                AddBlankLines(blocks, previous, start, blocks[^1].NodeId, gapsAreBlank);
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

    private static CodeBlockVm ConvertCodeBlock(CodeBlock block)
    {
        var info = (block as FencedCodeBlock)?.Info;
        return new(info, ParseLanguage(info), GetLeafText(block));
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
        else
        {
            state = (emphasis.DelimiterChar, emphasis.DelimiterCount) switch
            {
                ('~', 2) => state with { Strike = true },
                ('~', 1) => state with { Subscript = true },
                ('^', 1) => state with { Superscript = true },
                ('=', 2) => state with { Marked = true },
                ('+', 2) => state with { Inserted = true },
                ('"', 2) => state with { Cite = true },
                _ => state
            };
        }

        return state;
    }

    private IReadOnlyList<InlineVm> GetRawLines(LeafBlock leaf)
    {
        var inlines = new List<InlineVm>();

        for (var i = 0; i < leaf.Lines.Count; i++)
        {
            if (i > 0)
            {
                inlines.Add(new LineBreakVm());
            }

            var line = leaf.Lines.Lines[i];
            inlines.Add(new RunVm(line.Slice.ToString().TrimEnd('\r')) { Source = GetLineSpan(line.Line) });
        }

        return inlines;
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

    /// <summary>Строки блока через <c>\n</c>, какими бы ни были переводы строк исходника.</summary>
    private static string GetLeafText(LeafBlock block)
    {
        return block.Lines.ToString().Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
    }

    private static Uri? TryCreateUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
    }

    private IReadOnlyList<BlockVm> ConvertBlock(
        Block block,
        int listDepth)
    {
        foreach (var extension in _extensions)
        {
            if (extension.ConvertBlock(block, new(this, listDepth)) is { } custom)
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
                return [Stamp(new HeadingVm(heading.Level, ConvertInlines(heading.Inline), heading.TryGetAttributes()?.Id), block)];

            case ThematicBreakBlock:
                return [Stamp(new ThematicBreakVm(), block)];

            case YamlFrontMatterBlock:
                return [];

            case CodeBlock code:
                return [Stamp(ConvertCodeBlock(code), block)];

            case AlertBlock alert:
                return [Stamp(new CalloutVm(alert.Kind.ToString().ToLowerInvariant(), [], ConvertBlocks(alert, listDepth)), block)];

            case QuoteBlock quote:
                return [Stamp(new QuoteVm(ConvertBlocks(quote, listDepth)), block)];

            case DefinitionList definitions:
                return [Stamp(ConvertDefinitions(definitions, listDepth), block)];

            case DefinitionTerm:
                return [];

            case ListBlock list:
                return [Stamp(ConvertList(list, listDepth), block)];

            case Table table:
                return [Stamp(ConvertTable(table), block)];

            case Footnote footnote:
                return [Stamp(new FootnoteVm(footnote.Order, footnote.Label ?? string.Empty, ConvertBlocks(footnote, listDepth)), block)];

            case Figure figure:
                return [Stamp(new FigureVm(ConvertBlocks(figure, listDepth)), block)];

            case FigureCaption caption:
                return [Stamp(new FigureCaptionVm(ConvertInlines(caption.Inline)), block)];

            case FooterBlock footer:
                return [Stamp(new FooterVm(ConvertBlocks(footer, listDepth)), block)];

            case HtmlBlock { Type: HtmlBlockType.Comment }:
                return [];

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
        NodeVm stamped = node;
        if (stamped.Source.IsEmpty)
        {
            stamped = stamped with { Source = GetSpan(source) };
        }

        if (stamped.NodeId == 0)
        {
            stamped = stamped with { NodeId = _identity.IdOf(source) };
        }

        return (T)_identity.Reuse(stamped);
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
                var previous = items[^1];
                var previousId = previous.Blocks is [.., var last] ? last.NodeId : _identity.IdOf(previous.Item);
                AddBlankLines(previous.Blocks, previousEnd, itemStart, previousId);
            }

            var bullet = Bullets[listDepth % Bullets.Length];
            var marker = ordered ? OrderedMarker(start + items.Count, list.BulletType) + list.OrderedDelimiter : bullet;
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
        var rows = new List<(TableRow Row, bool IsHeader, List<TableCellVm> Cells)>();
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
                    cells.Add(Stamp(ConvertCell(cell), cell));
                }
            }

            maxColumns = Math.Max(maxColumns, cells.Count);
            rows.Add((row, r == 0, cells));
        }

        foreach (var (_, _, cells) in rows)
        {
            while (cells.Count < maxColumns)
            {
                cells.Add(new([]));
            }
        }

        var alignments = table.ColumnDefinitions
            .Select(column => column.Alignment switch
            {
                TableColumnAlign.Left => TableAlignment.Left,
                TableColumnAlign.Center => TableAlignment.Center,
                TableColumnAlign.Right => TableAlignment.Right,
                _ => TableAlignment.Default
            })
            .ToList();

        return new(maxColumns, rows.Select(entry => Stamp(new TableRowVm(entry.IsHeader, entry.Cells), entry.Row)).ToList())
        {
            Alignments = alignments
        };
    }

    /// <summary>Список определений: термины пункта — строками одного текста, определения — блоками.</summary>
    private DefinitionListVm ConvertDefinitions(DefinitionList list, int listDepth)
    {
        var items = new List<(List<InlineVm> Terms, List<BlockVm> Blocks, DefinitionItem Item)>();
        var previousEnd = 0;

        foreach (var item in list.OfType<DefinitionItem>())
        {
            var start = GetLineAt(item.Span.Start);
            if (items.Count > 0)
            {
                var previous = items[^1];
                var previousId = previous.Blocks is [.., var last] ? last.NodeId : _identity.IdOf(previous.Item);
                AddBlankLines(previous.Blocks, previousEnd, start, previousId);
            }

            var terms = new List<InlineVm>();
            foreach (var term in item.OfType<DefinitionTerm>())
            {
                if (terms.Count > 0)
                {
                    terms.Add(new LineBreakVm());
                }

                terms.AddRange(ConvertInlines(term.Inline));
            }

            items.Add((terms, ConvertBlocks(item, listDepth), item));
            previousEnd = GetLines(item).End;
        }

        return new(items.Select(entry => Stamp(new DefinitionItemVm(entry.Terms, entry.Blocks), entry.Item)).ToList());
    }

    /// <summary>Ячейка из одного абзаца — строкой текста, ячейка сетки-таблицы с другими блоками — блоками.</summary>
    private TableCellVm ConvertCell(TableCell cell)
        => cell is [] or [ParagraphBlock]
            ? new(GetCellInlines(cell))
            : new([]) { Blocks = ConvertBlocks(cell, listDepth: 0, gapsAreBlank: true) };

    private IReadOnlyList<InlineVm> GetCellInlines(TableCell cell)
    {
        var paragraph = cell.Descendants<ParagraphBlock>().FirstOrDefault();
        return paragraph?.Inline is { } inline
            ? ConvertInlines(inline)
            : [];
    }

    /// <summary>
    /// Инлайны подряд. Открывающий тег строчного HTML меняет начертание соседей до своего
    /// закрывающего.
    /// </summary>
    private void WalkInline(Inline? current, InlineState state, List<InlineVm> output)
    {
        var openTags = new List<(string Name, InlineState Before)>();

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

                case LineBreakInline lineBreak:
                    output.Add(Stamp(new LineBreakVm(lineBreak.IsHard), node));
                    break;

                case CodeInline code:
                    AddText(code.Content ?? string.Empty, state with { Code = true }, output, node);
                    break;

                case AutolinkInline autolink:
                    AddLink(autolink.Url, null, null, state, output, node);
                    break;

                case HtmlInline html:
                    state = ApplyHtml(html, state, openTags, output);
                    break;

                case HtmlEntityInline entity:
                    AddText(entity.Transcoded.ToString(), state, output, node);
                    break;

                case TaskList task:
                    output.Add(Stamp(new TaskVm(task.Checked), node));
                    break;

                case FootnoteLink footnoteLink:
                    output.Add(Stamp(new FootnoteLinkVm(footnoteLink.Footnote.Order, footnoteLink.Index, footnoteLink.IsBackLink), node));
                    break;

                case AbbreviationInline abbreviation:
                    AddText(
                        abbreviation.Abbreviation.Label ?? GetSourceText(node.Span),
                        state with { Abbreviation = abbreviation.Abbreviation.Text.ToString() },
                        output,
                        node);
                    break;

                case SmartyPant smartyPant:
                    AddText(SmartyPantText(smartyPant), state, output, node);
                    break;

                case LinkInline { IsImage: true } image:
                    AddImage(image, state, output);
                    break;

                case LinkInline link:
                    AddLink(link.Url, link.Title, link.FirstChild, state, output, node);
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

    /// <summary>Начертание, которое даёт открывающий тег; тег не про начертание — <c>null</c>.</summary>
    private static InlineState? Formatting(HtmlTag tag, InlineState state)
        => tag.Name switch
        {
            "b" or "strong" => state with { Bold = true },
            "i" or "em" => state with { Italic = true },
            "s" or "del" or "strike" => state with { Strike = true },
            "code" => state with { Code = true },
            "kbd" => state with { Kbd = true },
            "sub" => state with { Subscript = true },
            "sup" => state with { Superscript = true },
            "mark" => state with { Marked = true },
            "ins" or "u" => state with { Inserted = true },
            "cite" => state with { Cite = true },
            "a" when tag.Attributes.TryGetValue("href", out var href) => state with
            {
                Hyperlink = TryCreateUri(href),
                Href = href,
                LinkTitle = tag.Attributes.GetValueOrDefault("title")
            },
            "a" => state,
            _ => null
        };

    /// <summary>Строчный HTML-тег; возвращает начертание для следующих соседей.</summary>
    private InlineState ApplyHtml(
        HtmlInline html,
        InlineState state,
        List<(string Name, InlineState Before)> openTags,
        List<InlineVm> output)
    {
        if (html.Tag.StartsWith("<!--", StringComparison.Ordinal))
        {
            return state;
        }

        switch (HtmlTag.Parse(html.Tag))
        {
            case { Name: "br" }:
                output.Add(Stamp(new LineBreakVm(IsHard: true), html));
                return state;

            case { Name: "img", IsClosing: false } image:
                output.Add(Stamp(
                    new ImageVm(
                        image.Attributes.GetValueOrDefault("src") ?? string.Empty,
                        image.Attributes.GetValueOrDefault("alt") ?? string.Empty)
                    {
                        Title = image.Attributes.GetValueOrDefault("title"),
                        Width = Pixels(image.Attributes.GetValueOrDefault("width")),
                        Height = Pixels(image.Attributes.GetValueOrDefault("height")),
                        Href = state.Href,
                        Hyperlink = state.Hyperlink
                    },
                    html));
                return state;

            case { IsClosing: true } closing when openTags.FindLastIndex(open => open.Name == closing.Name) is var index and >= 0:
                var before = openTags[index].Before;
                openTags.RemoveRange(index, openTags.Count - index);
                return before;

            case { IsClosing: false } opening when Formatting(opening, state) is { } formatted:
                if (!opening.IsSelfClosing)
                {
                    openTags.Add((opening.Name, state));
                }

                return formatted;

            default:
                AddText(html.Tag, state, output, html);
                return state;
        }
    }

    private void AddImage(LinkInline image, InlineState state, List<InlineVm> output)
    {
        var attributes = image.TryGetAttributes()?.Properties;
        string? Attribute(string name) => attributes?.FirstOrDefault(pair => pair.Key == name).Value;

        output.Add(Stamp(
            new ImageVm(image.Url ?? string.Empty, PlainText(image.FirstChild))
            {
                Title = string.IsNullOrEmpty(image.Title) ? null : image.Title,
                Width = Pixels(Attribute("width")),
                Height = Pixels(Attribute("height")),
                Href = state.Href,
                Hyperlink = state.Hyperlink
            },
            image));
    }

    /// <summary>Размер из атрибута: число пикселей, можно с <c>px</c>.</summary>
    private static double? Pixels(string? value)
    {
        var number = value?.Trim().TrimEnd('x', 'X').TrimEnd('p', 'P');
        return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels) && pixels > 0
            ? pixels
            : null;
    }

    /// <summary>Текст инлайнов без разметки — alt картинки.</summary>
    private static string PlainText(Inline? first)
    {
        var text = new StringBuilder();
        for (var node = first; node is not null; node = node.NextSibling)
        {
            switch (node)
            {
                case LiteralInline literal:
                    text.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    text.Append(code.Content);
                    break;
                case ContainerInline container:
                    text.Append(PlainText(container.FirstChild));
                    break;
            }
        }

        return text.ToString();
    }

    private bool TryConvertByExtension(Inline node, List<InlineVm> output)
    {
        foreach (var extension in _extensions)
        {
            if (extension.ConvertInline(node, new(this, listDepth: 0)) is { } custom)
            {
                output.Add(Stamp(custom, node));
                return true;
            }
        }

        return false;
    }

    private void AddLink(
        string? url,
        string? title,
        Inline? firstChild,
        InlineState state,
        List<InlineVm> output,
        Inline node)
    {
        var linkedState = state with
        {
            Hyperlink = TryCreateUri(url),
            Href = url,
            LinkTitle = string.IsNullOrEmpty(title) ? null : title
        };

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
                state.Href,
                state.LinkTitle,
                state.Kbd,
                state.Subscript,
                state.Superscript,
                state.Marked,
                state.Inserted,
                state.Cite,
                state.Abbreviation),
            node));
    }

    /// <summary>Замена SmartyPants по таблице из настройки разбора; таблица там — HTML-сущностями.</summary>
    private string SmartyPantText(SmartyPant smartyPant)
        => _smartyPants is not null && _smartyPants.TryGetValue(smartyPant.Type, out var replacement)
            ? WebUtility.HtmlDecode(replacement)
            : GetSourceText(smartyPant.Span);

    /// <summary>Маркер пункта нумерованного списка: число, буква или римская цифра по виду списка.</summary>
    private static string OrderedMarker(int number, char bulletType)
        => bulletType switch
        {
            'a' => Letters(number),
            'A' => Letters(number).ToUpperInvariant(),
            'i' => Roman(number),
            'I' => Roman(number).ToUpperInvariant(),
            _ => number.ToString(CultureInfo.InvariantCulture)
        };

    /// <summary>Номер буквами: a…z, затем aa, ab…</summary>
    private static string Letters(int number)
    {
        if (number <= 0)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        var letters = new StringBuilder();
        for (var rest = number; rest > 0; rest = (rest - 1) / 26)
        {
            letters.Insert(0, (char)('a' + (rest - 1) % 26));
        }

        return letters.ToString();
    }

    /// <summary>Номер строчными римскими цифрами; ноль и отрицательные — арабскими.</summary>
    private static string Roman(int number)
    {
        if (number <= 0)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        (int Value, string Digits)[] numerals =
        [
            (1000, "m"), (900, "cm"), (500, "d"), (400, "cd"), (100, "c"), (90, "xc"),
            (50, "l"), (40, "xl"), (10, "x"), (9, "ix"), (5, "v"), (4, "iv"), (1, "i")
        ];

        var roman = new StringBuilder();
        var rest = number;
        foreach (var (value, digits) in numerals)
        {
            while (rest >= value)
            {
                roman.Append(digits);
                rest -= value;
            }
        }

        return roman.ToString();
    }

    /// <summary>Отступы на месте пустых строк; идентификатор отступа держится за блок перед ним.</summary>
    private void AddBlankLines(List<BlockVm> blocks, int afterLine, int beforeLine, long previousId, bool gapsAreBlank = false)
    {
        var index = 0;
        for (var line = afterLine + 1; line < beforeLine && line < _lineStarts.Count; line++)
        {
            if (gapsAreBlank || IsBlankLine(line))
            {
                var blank = new BlankLineVm { Source = GetLineSpan(line), NodeId = _identity.BlankLineId(previousId, index++) };
                blocks.Add(_identity.Reuse(blank));
            }
        }
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

    private readonly record struct InlineState(
        bool Bold = false,
        bool Italic = false,
        bool Strike = false,
        bool Code = false,
        Uri? Hyperlink = null,
        string? Href = null,
        string? LinkTitle = null,
        bool Kbd = false,
        bool Subscript = false,
        bool Superscript = false,
        bool Marked = false,
        bool Inserted = false,
        bool Cite = false,
        string? Abbreviation = null);
}