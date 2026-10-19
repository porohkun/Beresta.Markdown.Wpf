namespace Beresta.Markdown.Wpf;

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Extensions;
using Identity;
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

internal sealed partial class ModelBuilder
{
    /// <summary>Сколько контейнеров друг в друге разворачивается; более глубокий — исходным текстом.</summary>
    public const int MaxDepth = 32;

    /// <summary>Сколько строчных узлов друг в друге обходится; более глубокий — исходным текстом.</summary>
    public const int MaxInlineDepth = 64;

    public static readonly MarkdownPipeline DefaultPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    private static readonly string[] Bullets = ["•", "◦", "▪"];

    /// <summary>Блочные теги HTML из README; HTML-блок, где все теги знакомы, рисуется по смыслу.</summary>
    private static readonly HashSet<string> HtmlBlockTags = ["details", "summary", "div", "p", "center", "h1", "h2", "h3", "h4", "h5", "h6"];

    /// <summary>Строчные теги HTML из README; <c>picture</c> и <c>source</c> пропускаются, остаётся их <c>img</c>.</summary>
    private static readonly HashSet<string> HtmlInlineTags =
    [
        "a", "img", "br", "picture", "source", "b", "strong", "i", "em", "s", "del", "strike", "code", "kbd", "sub", "sup", "mark", "ins", "u", "cite"
    ];

    private readonly string _source;
    private readonly List<int> _lineStarts;
    private readonly IReadOnlyList<MdExtension> _extensions;
    private readonly ModelIdentity _identity;
    private readonly IReadOnlyDictionary<SmartyPantType, string>? _smartyPants;
    private readonly Dictionary<HtmlBlock, int> _htmlParts = new(ReferenceEqualityComparer.Instance);
    private readonly ExtensionGuard _guard;
    private readonly IDictionary<object, MdExtension>? _owners;

    private int _depth;
    private int _inlineDepth;

    private ModelBuilder(
        string source,
        IReadOnlyList<MdExtension> extensions,
        ModelIdentity identity,
        IReadOnlyDictionary<SmartyPantType, string>? smartyPants,
        ExtensionGuard guard,
        IDictionary<object, MdExtension>? owners)
    {
        _source = source;
        _lineStarts = FindLineStarts(source);
        _extensions = extensions;
        _identity = identity;
        _smartyPants = smartyPants;
        _guard = guard;
        _owners = owners;
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
    /// и отдаёт текст front matter без разбора; нет его — <c>null</c>. Без <paramref name="commit"/> память
    /// разборов запоминает этот разбор, только когда её <see cref="ModelIdentity.Commit"/> вызовут отдельно.
    /// Код расширений идёт через <paramref name="guard"/>; <paramref name="owners"/> получает, какое
    /// расширение создало каждую его вьюмодель.
    /// </summary>
    public static IReadOnlyList<BlockVm> Parse(
        string markdown,
        MarkdownPipeline? pipeline,
        IReadOnlyList<MdExtension>? extensions,
        ModelIdentity? identity,
        out string? frontMatter,
        bool commit = true,
        ExtensionGuard? guard = null,
        IDictionary<object, MdExtension>? owners = null)
    {
        markdown ??= string.Empty;
        identity ??= new();
        pipeline ??= DefaultPipeline;

        var document = Markdown.Parse(markdown, pipeline);
        identity.Match(document, markdown);
        frontMatter = document is [YamlFrontMatterBlock yaml, ..] ? yaml.Lines.ToString() : null;

        var smartyPants = pipeline.Extensions.Find<SmartyPantsExtension>()?.Options.Mapping;
        var builder = new ModelBuilder(markdown, extensions ?? [], identity, smartyPants, guard ?? ExtensionGuard.Silent, owners);
        var blocks = builder.ConvertBlocks(document, listDepth: 0);
        if (commit)
        {
            identity.Commit();
        }

        return blocks;
    }

    /// <summary>Документ из строк исходника без разбора: непустые строки подряд — абзацем, пустые — отступами.</summary>
    public static IReadOnlyList<BlockVm> SourceDocument(string markdown)
        => new ModelBuilder(markdown ?? string.Empty, [], new(), null, ExtensionGuard.Silent, null).SourceLines();

    /// <summary>
    /// Блоки контейнера с отступами на месте пустых строк. Определения сносок, которые Markdig
    /// собирает в конец документа, встают туда, где они написаны. HTML из README, который Markdig
    /// режет на соседние блоки, собирается обратно. В ячейке сетки-таблицы
    /// <paramref name="gapsAreBlank"/>: строка между её блоками пуста внутри столбца, даже если в
    /// соседних столбцах на ней текст.
    /// </summary>
    internal List<BlockVm> ConvertBlocks(
        ContainerBlock container,
        int listDepth,
        bool gapsAreBlank = false)
    {
        _depth++;
        try
        {
            var pieces = container
                .SelectMany(child => child is FootnoteGroup group ? group.AsEnumerable() : new[] { child })
                .OrderBy(child => child.Line)
                .SelectMany(child => PiecesOf(child, listDepth))
                .ToList();

            var index = 0;
            return ConvertPieces(pieces, ref index, [], listDepth, gapsAreBlank);
        }
        finally
        {
            _depth--;
        }
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
        var block = node as Block;
        var end = Math.Min(block is null ? node.Span.End : EndOf(block), _source.Length - 1);
        var (startLine, endLine) = block is null
            ? (GetLineAt(start), GetLineAt(end))
            : GetLines(block);

        return new(start, end - start + 1, startLine, endLine);
    }

    internal string GetSource(MarkdownObject node)
    {
        return GetSourceText(node is Block block ? new(node.Span.Start, EndOf(block)) : node.Span);
    }

    private static CodeBlockVm ConvertCodeBlock(CodeBlock block)
    {
        var info = (block as FencedCodeBlock)?.Info;
        return new(info, ParseLanguage(info), CodeOf(block)) { IsFenced = block is FencedCodeBlock };
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

    /// <summary>
    /// Строки кода через <c>\n</c>, какими бы ни были переводы строк исходника. Пустые строки в конце
    /// остаются только перед закрывающей оградой.
    /// </summary>
    private static string CodeOf(CodeBlock block)
    {
        var code = block.Lines.ToString().Replace("\r\n", "\n").Replace('\r', '\n');
        return block is FencedCodeBlock { ClosingFencedCharCount: > 0 } ? code : code.TrimEnd('\n');
    }

    /// <summary>
    /// Последний знак блока в исходнике. Код с отступом и код без закрывающей ограды кончаются своей
    /// последней непустой строкой, контейнер — не раньше своего последнего блока.
    /// </summary>
    private static int EndOf(Block block)
    {
        switch (block)
        {
            case ContainerBlock { LastChild: { } last }:
                return Math.Max(block.Span.End, EndOf(last));

            case CodeBlock code when code.GetType() == typeof(CodeBlock) || code is FencedCodeBlock { ClosingFencedCharCount: 0 }:
                for (var i = code.Lines.Count - 1; i >= 0; i--)
                {
                    var slice = code.Lines.Lines[i].Slice;
                    if (!slice.IsEmptyOrWhitespace())
                    {
                        return slice.End;
                    }
                }

                return block.Span.End;

            default:
                return block.Span.End;
        }
    }

    private static Uri? TryCreateUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
    }

    /// <summary>Пробелы HTML: подряд идущие — одним пробелом, в начале строки и в конце — никаких.</summary>
    private static List<InlineVm> CollapseHtmlSpaces(List<InlineVm> inlines)
    {
        var result = new List<InlineVm>();
        var lineStart = true;

        foreach (var inline in inlines)
        {
            if (inline is not RunVm run)
            {
                if (inline is LineBreakVm)
                {
                    TrimEnd();
                }

                result.Add(inline);
                lineStart = inline is LineBreakVm;
                continue;
            }

            var text = HtmlSpaces().Replace(run.Text, " ");
            if (lineStart || result is [.., RunVm { Text: [.., ' '] }])
            {
                text = text.TrimStart(' ');
            }

            if (text.Length > 0)
            {
                result.Add(run with { Text = text });
                lineStart = false;
            }
        }

        TrimEnd();
        return result;

        void TrimEnd()
        {
            if (result is [.., RunVm { Text: [.., ' '] } last])
            {
                var trimmed = last.Text.TrimEnd(' ');
                result[^1] = last with { Text = trimmed };
                if (trimmed.Length == 0)
                {
                    result.RemoveAt(result.Count - 1);
                }
            }
        }
    }

    /// <summary>Выравнивание по атрибуту <c>align</c>, у <c>&lt;center&gt;</c> — по центру; без выравнивания — <c>null</c>.</summary>
    private static BlockAlignment? AlignmentOf(HtmlTag tag)
        => tag.Name == "center"
            ? BlockAlignment.Center
            : tag.Attributes.GetValueOrDefault("align")?.Trim().ToLowerInvariant() switch
            {
                "left" => BlockAlignment.Left,
                "center" or "middle" => BlockAlignment.Center,
                "right" => BlockAlignment.Right,
                "justify" => BlockAlignment.Justify,
                _ => null
            };

    private static MdSourceSpan SpanOf(Piece piece) => new(piece.Start, piece.End - piece.Start, piece.StartLine, piece.EndLine);

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

    /// <summary>Картинка тега <c>&lt;img&gt;</c>; внутри ссылки — картинка-ссылка.</summary>
    private static ImageVm ImageOf(HtmlTag image, InlineState state)
        => new(image.Attributes.GetValueOrDefault("src") ?? string.Empty, image.Attributes.GetValueOrDefault("alt") ?? string.Empty)
        {
            Title = image.Attributes.GetValueOrDefault("title"),
            Width = Pixels(image.Attributes.GetValueOrDefault("width")),
            Height = Pixels(image.Attributes.GetValueOrDefault("height")),
            Href = state.Href,
            Hyperlink = state.Hyperlink,
            LinkTitle = state.LinkTitle
        };

    /// <summary>Размер из атрибута: число пикселей, можно с <c>px</c>.</summary>
    private static double? Pixels(string? value)
    {
        var number = value?.Trim().TrimEnd('x', 'X').TrimEnd('p', 'P');
        return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var pixels) && pixels > 0
            ? pixels
            : null;
    }

    /// <summary>Текст инлайнов без разметки — alt картинки.</summary>
    internal static string PlainText(Inline? first)
    {
        var text = new StringBuilder();
        var next = new Stack<Inline>();
        if (first is not null)
        {
            next.Push(first);
        }

        while (next.TryPop(out var node))
        {
            if (node.NextSibling is { } sibling)
            {
                next.Push(sibling);
            }

            switch (node)
            {
                case LiteralInline literal:
                    text.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    text.Append(code.Content);
                    break;
                case ContainerInline { FirstChild: { } child }:
                    next.Push(child);
                    break;
            }
        }

        return text.ToString();
    }

    private static RunVm Run(string text, InlineState state)
        => new(
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
            state.Abbreviation);

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

    [GeneratedRegex("[ \t\r\n\f]+")]
    private static partial Regex HtmlSpaces();

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

    /// <summary>Непустые строки исходника подряд — абзацем по строке текста на строку, пустые между ними — отступами.</summary>
    private List<BlockVm> SourceLines()
    {
        var blocks = new List<BlockVm>();
        var lastText = -1;
        for (var line = 0; line < _lineStarts.Count; line++)
        {
            if (IsWhiteSpaceLine(line))
            {
                continue;
            }

            var first = line;
            while (line + 1 < _lineStarts.Count && !IsWhiteSpaceLine(line + 1))
            {
                line++;
            }

            for (var blank = lastText + 1; lastText >= 0 && blank < first; blank++)
            {
                blocks.Add(new BlankLineVm { Source = GetLineSpan(blank) });
            }

            var inlines = new List<InlineVm>();
            for (var row = first; row <= line; row++)
            {
                if (row > first)
                {
                    inlines.Add(new LineBreakVm());
                }

                var start = _lineStarts[row];
                var end = LineEnd(row);
                inlines.Add(new RunVm(_source[start..end]) { Source = new(start, end - start, row, row) });
            }

            blocks.Add(new ParagraphVm(inlines) { Source = new(_lineStarts[first], LineEnd(line) - _lineStarts[first], first, line) });
            lastText = line;
        }

        return blocks;
    }

    /// <summary>Контейнер глубже предела вложенности — абзацем его исходника.</summary>
    private List<BlockVm> BlockSource(Block block)
    {
        return block.Span.Start >= 0 && SourceParagraph(block.Span.Start, EndOf(block) + 1) is { } paragraph
            ? [Stamp(paragraph, block)]
            : [];
    }

    /// <summary>
    /// Абзац из куска исходника — строкой текста на строку. Строки после первой теряют префикс
    /// контейнеров вокруг: пробелы и <c>&gt;</c> до столбца, с которого начинается кусок.
    /// </summary>
    private ParagraphVm? SourceParagraph(int start, int end)
    {
        end = Math.Min(end, _source.Length);
        while (start < end && char.IsWhiteSpace(_source[start]))
        {
            start++;
        }

        while (end > start && char.IsWhiteSpace(_source[end - 1]))
        {
            end--;
        }

        if (start == end)
        {
            return null;
        }

        var firstLine = GetLineAt(start);
        var lastLine = GetLineAt(end - 1);
        var column = start - _lineStarts[firstLine];
        var inlines = new List<InlineVm>();
        for (var line = firstLine; line <= lastLine; line++)
        {
            var from = start;
            if (line > firstLine)
            {
                inlines.Add(new LineBreakVm());
                from = _lineStarts[line];
                var prefixEnd = Math.Min(from + column, end);
                while (from < prefixEnd && _source[from] is ' ' or '\t' or '>')
                {
                    from++;
                }
            }

            var to = line == lastLine ? end : LineEnd(line);
            if (to > from)
            {
                inlines.Add(new RunVm(_source[from..to]) { Source = new(from, to - from, line, line) });
            }
        }

        return new(inlines) { Source = new(start, end - start, firstLine, lastLine) };
    }

    private IReadOnlyList<BlockVm> ConvertBlock(
        Block block,
        int listDepth)
        => block is ContainerBlock and not LinkReferenceDefinitionGroup && _depth > MaxDepth
            ? BlockSource(block)
            : ConvertByExtension(block, listDepth) ?? ConvertBuiltInBlock(block, listDepth);

    /// <summary>Блоки расширения из узла; первый берёт идентификатор узла, следующие — производные от него.</summary>
    private IReadOnlyList<BlockVm>? ConvertByExtension(
        Block block,
        int listDepth)
    {
        foreach (var extension in _extensions)
        {
            var converted = _guard.Call(
                extension,
                () => extension
                    .ConvertBlocks(block, new(this, listDepth))
                    ?.Select((part, index) => (BlockVm)Owned(StampPart(part, block, index), extension))
                    .ToList(),
                null);

            if (converted is not null)
            {
                return converted;
            }
        }

        return null;
    }

    /// <summary>Запоминает, какое расширение создало вьюмодель.</summary>
    private NodeVm Owned(NodeVm node, MdExtension extension)
    {
        if (_owners is not null)
        {
            _owners[node] = extension;
        }

        return node;
    }

    /// <summary><paramref name="index"/>-я часть узла: первая — с идентификатором узла, следующие — с производными.</summary>
    private NodeVm StampPart(NodeVm part, MarkdownObject node, int index)
    {
        if (index > 0 && part.NodeId == 0)
        {
            part = part with { NodeId = _identity.PartId(_identity.IdOf(node), index) };
        }

        return Stamp(part, node);
    }

    private IReadOnlyList<BlockVm> ConvertBuiltInBlock(
        Block block,
        int listDepth)
    {
        switch (block)
        {
            case LinkReferenceDefinitionGroup or LinkReferenceDefinition:
                return [];

            case ParagraphBlock { Inline: null or { FirstChild: null } }:
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

    /// <summary>
    /// Вьюмодель из HTML от куска <paramref name="first"/> до куска <paramref name="last"/>, если
    /// позиции у неё ещё нет. Первая вьюмодель HTML-блока берёт его идентификатор, следующие —
    /// производные от него.
    /// </summary>
    private T StampHtml<T>(T node, Piece first, Piece last)
        where T : NodeVm
    {
        var owner = first.Owner!;
        var part = _htmlParts.GetValueOrDefault(owner);
        _htmlParts[owner] = part + 1;

        var id = _identity.IdOf(owner);
        NodeVm stamped = node;
        stamped = stamped with
        {
            Source = stamped.Source.IsEmpty ? new(first.Start, last.End - first.Start, first.StartLine, last.EndLine) : stamped.Source,
            NodeId = part == 0 ? id : _identity.PartId(id, part)
        };

        return (T)_identity.Reuse(stamped);
    }

    /// <summary>Блок — одним куском; HTML-блок, все теги которого знакомы, — своими тегами и текстом.</summary>
    private IEnumerable<Piece> PiecesOf(Block block, int listDepth)
    {
        var (startLine, endLine) = GetLines(block);
        var whole = new Piece(block.Span.Start, EndOf(block) + 1, startLine, endLine) { Block = block };
        if (block is not HtmlBlock html || html.Type == HtmlBlockType.Comment)
        {
            return [whole];
        }

        if (ConvertByExtension(html, listDepth) is { } custom)
        {
            return [whole with { Converted = custom }];
        }

        return HtmlPieces(html) ?? [whole with { Converted = ConvertBuiltInBlock(html, listDepth) }];
    }

    /// <summary>Теги и текст HTML-блока кусками с позициями в исходнике; блок с незнакомым тегом — <c>null</c>.</summary>
    private List<Piece>? HtmlPieces(HtmlBlock html)
    {
        var text = new StringBuilder();
        var lines = new List<(int Offset, int Position)>();
        for (var i = 0; i < html.Lines.Count; i++)
        {
            if (i > 0)
            {
                text.Append('\n');
            }

            var slice = html.Lines.Lines[i].Slice;
            lines.Add((text.Length, slice.Start));
            text.Append(slice.ToString());
        }

        var tokens = HtmlToken.Split(text.ToString());
        if (tokens.Any(token => token.IsTag && (token.Tag is not { } tag || (!HtmlBlockTags.Contains(tag.Name) && !HtmlInlineTags.Contains(tag.Name)))))
        {
            return null;
        }

        return tokens
            .Where(token => !token.IsComment)
            .Select(token =>
            {
                var start = PositionOf(token.Start);
                var end = PositionOf(token.End - 1) + 1;
                return new Piece(start, end, GetLineAt(start), GetLineAt(end - 1))
                {
                    Tag = token.Tag,
                    Text = token.IsTag ? null : token.Text,
                    Owner = html
                };
            })
            .ToList();

        int PositionOf(int offset)
        {
            var line = lines.FindLast(line => line.Offset <= offset);
            return line.Position + offset - line.Offset;
        }
    }

    /// <summary>
    /// Блоки из кусков подряд, пустые строки между ними — отступами. Останавливается на закрывающем
    /// теге элемента из <paramref name="open"/> и оставляет его этому элементу.
    /// </summary>
    private List<BlockVm> ConvertPieces(
        List<Piece> pieces,
        ref int index,
        List<string> open,
        int listDepth,
        bool gapsAreBlank)
    {
        var blocks = new List<BlockVm>();
        int? previousEnd = null;

        while (index < pieces.Count)
        {
            var piece = pieces[index];
            if (piece.Tag is { IsClosing: true } closing)
            {
                if (open.Contains(closing.Name))
                {
                    break;
                }

                index++;
                continue;
            }

            IReadOnlyList<BlockVm> converted;
            if (piece.Block is { } block)
            {
                index++;
                converted = piece.Converted ?? ConvertBlock(block, listDepth);
            }
            else
            {
                converted = ConvertHtml(pieces, ref index, open, listDepth);
            }

            if (converted.Count == 0)
            {
                continue;
            }

            if (previousEnd is { } previous)
            {
                AddBlankLines(blocks, previous, piece.StartLine, blocks[^1].NodeId, gapsAreBlank);
            }

            blocks.AddRange(converted);
            previousEnd = pieces[index - 1].EndLine;
        }

        return blocks;
    }

    /// <summary>Элемент HTML из README, который начинается с куска <paramref name="index"/>, — блоками.</summary>
    private IReadOnlyList<BlockVm> ConvertHtml(
        List<Piece> pieces,
        ref int index,
        List<string> open,
        int listDepth)
    {
        var first = pieces[index];
        switch (first.Tag)
        {
            case { Name: "details" or "div" or "p" or "center" or "summary" } when _depth > MaxDepth:
                return ElementSource(pieces, ref index, open);

            case { Name: "details" } details:
                index++;
                var summary = ConvertSummary(pieces, ref index);
                var content = ConvertElement(pieces, ref index, open, details.Name, listDepth);
                return [StampHtml(new DetailsVm(summary, content) { IsOpen = details.Attributes.ContainsKey("open") }, first, pieces[index - 1])];

            case { Name: "div" or "p" or "center" or "summary" } container:
                index++;
                var blocks = ConvertElement(pieces, ref index, open, container.Name, listDepth);
                return AlignmentOf(container) is { } alignment && blocks.Count > 0
                    ? [StampHtml(new AlignedVm(alignment, blocks), first, pieces[index - 1])]
                    : blocks;

            case { Name: ['h', >= '1' and <= '6'] } heading:
                index++;
                var title = ConvertHtmlInlines(pieces, ref index);
                if (index < pieces.Count && pieces[index].Tag is { IsClosing: true } end && end.Name == heading.Name)
                {
                    index++;
                }

                var headingVm = StampHtml(new HeadingVm(heading.Name[1] - '0', title, heading.Attributes.GetValueOrDefault("id")), first, pieces[index - 1]);
                return AlignmentOf(heading) is { } headingAlignment
                    ? [StampHtml(new AlignedVm(headingAlignment, [headingVm]), first, pieces[index - 1])]
                    : [headingVm];

            default:
                var start = index;
                var inlines = ConvertHtmlInlines(pieces, ref index);
                if (index == start)
                {
                    index++;
                    return [];
                }

                return inlines switch
                {
                    [] => [],
                    _ when inlines.All(inline => inline is LineBreakVm) => [StampHtml(new BlankLineVm(), first, pieces[index - 1])],
                    _ => [StampHtml(new ParagraphVm(inlines), first, pieces[index - 1])]
                };
        }
    }

    /// <summary>Блоки элемента HTML до его закрывающего тега; сам тег съедается.</summary>
    private List<BlockVm> ConvertElement(
        List<Piece> pieces,
        ref int index,
        List<string> open,
        string name,
        int listDepth)
    {
        _depth++;
        try
        {
            open.Add(name);
            var blocks = ConvertPieces(pieces, ref index, open, listDepth, gapsAreBlank: false);
            open.RemoveAt(open.Count - 1);

            if (index < pieces.Count && pieces[index].Tag is { IsClosing: true } closing && closing.Name == name)
            {
                index++;
            }

            return blocks;
        }
        finally
        {
            _depth--;
        }
    }

    /// <summary>
    /// Элемент HTML глубже предела вложенности, который начинается с куска <paramref name="index"/>, —
    /// абзацем его исходника вместе с закрывающим тегом.
    /// </summary>
    private List<BlockVm> ElementSource(
        List<Piece> pieces,
        ref int index,
        List<string> open)
    {
        var first = pieces[index];
        var nested = new List<string>();
        for (; index < pieces.Count; index++)
        {
            var tag = pieces[index].Tag;
            if (tag is { IsClosing: true } && nested.LastIndexOf(tag.Name) is var level and >= 0)
            {
                nested.RemoveRange(level, nested.Count - level);
                if (nested.Count == 0)
                {
                    index++;
                    break;
                }
            }
            else if (tag is { IsClosing: true } && open.Contains(tag.Name))
            {
                break;
            }
            else if (tag is { IsClosing: false, Name: "details" or "div" or "p" or "center" or "summary" })
            {
                nested.Add(tag.Name);
            }
        }

        var last = pieces[index - 1];
        return SourceParagraph(first.Start, last.End) is { } paragraph
            ? [StampHtml(paragraph, first, last)]
            : [];
    }

    /// <summary>Заголовок <c>&lt;summary&gt;</c> в начале <c>&lt;details&gt;</c>; нет его — пусто.</summary>
    private IReadOnlyList<InlineVm> ConvertSummary(List<Piece> pieces, ref int index)
    {
        var next = index;
        while (next < pieces.Count && pieces[next].Text is { } text && string.IsNullOrWhiteSpace(text))
        {
            next++;
        }

        if (next >= pieces.Count || pieces[next].Tag is not { Name: "summary", IsClosing: false })
        {
            return [];
        }

        index = next + 1;
        var summary = ConvertHtmlInlines(pieces, ref index);
        if (index < pieces.Count && pieces[index].Tag is { Name: "summary", IsClosing: true })
        {
            index++;
        }

        return summary;
    }

    /// <summary>
    /// Строчное содержимое HTML подряд: текст, картинки, ссылки, начертания, переносы — до блока
    /// Markdig или блочного тега.
    /// </summary>
    private List<InlineVm> ConvertHtmlInlines(List<Piece> pieces, ref int index)
    {
        var output = new List<InlineVm>();
        var state = new InlineState();
        var openTags = new List<(string Name, InlineState Before)>();

        for (; index < pieces.Count; index++)
        {
            var piece = pieces[index];
            if (piece.Block is not null || (piece.Tag is { } blockTag && HtmlBlockTags.Contains(blockTag.Name)))
            {
                break;
            }

            if (piece.Text is { } text)
            {
                output.Add(Run(WebUtility.HtmlDecode(text), state) with { Source = SpanOf(piece) });
                continue;
            }

            switch (piece.Tag)
            {
                case { Name: "br" }:
                    output.Add(new LineBreakVm(IsHard: true) { Source = SpanOf(piece) });
                    break;

                case { Name: "img", IsClosing: false } image:
                    output.Add(ImageOf(image, state) with { Source = SpanOf(piece) });
                    break;

                case { IsClosing: true } closing when openTags.FindLastIndex(tag => tag.Name == closing.Name) is var opened and >= 0:
                    state = openTags[opened].Before;
                    openTags.RemoveRange(opened, openTags.Count - opened);
                    break;

                case { IsClosing: false } opening when Formatting(opening, state) is { } formatted:
                    if (!opening.IsSelfClosing)
                    {
                        openTags.Add((opening.Name, state));
                    }

                    state = formatted;
                    break;
            }
        }

        return CollapseHtmlSpaces(output);
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

    /// <summary>
    /// Таблица; ячейка встаёт в свой столбец, объединённая занимает несколько, а пропуски — место под
    /// ячейкой строки выше, конец короткой строки — заполняются пустыми ячейками.
    /// </summary>
    private TableVm ConvertTable(Table table)
    {
        var rows = new List<(TableRow Row, List<TableCellVm> Cells, int Width)>();
        var maxColumns = table.ColumnDefinitions.Count;

        foreach (var row in table.OfType<TableRow>())
        {
            var cells = new List<TableCellVm>();
            var width = 0;
            foreach (var cell in row.OfType<TableCell>())
            {
                for (; width < cell.ColumnIndex; width++)
                {
                    cells.Add(new([]));
                }

                var span = Math.Max(1, cell.ColumnSpan);
                cells.Add(Stamp(ConvertCell(cell) with { ColumnSpan = span }, cell));
                width += span;
            }

            maxColumns = Math.Max(maxColumns, width);
            rows.Add((row, cells, width));
        }

        foreach (var (_, cells, width) in rows)
        {
            for (var column = width; column < maxColumns; column++)
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

        return new(maxColumns, rows.Select(entry => Stamp(new TableRowVm(entry.Row.IsHeader, entry.Cells), entry.Row)).ToList())
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

        _inlineDepth++;
        try
        {
            for (var node = current; node is not null; node = node.NextSibling)
            {
                if (TryConvertByExtension(node, state, output))
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
                        AddText(autolink.Url, Linked(state, autolink.IsEmail ? $"mailto:{autolink.Url}" : autolink.Url, null), output, node);
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

                    case ContainerInline when _inlineDepth > MaxInlineDepth:
                        AddText(GetSourceText(node.Span), state, output, node);
                        break;

                    case LinkInline link:
                        AddLink(link.GetDynamicUrl?.Invoke() ?? link.Url, link.Title, link.FirstChild, state, output, node);
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
        finally
        {
            _inlineDepth--;
        }
    }

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
                output.Add(Stamp(ImageOf(image, state), html));
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
                Hyperlink = state.Hyperlink,
                LinkTitle = state.LinkTitle
            },
            image));
    }

    /// <summary>Инлайны расширения из узла; первый берёт идентификатор узла, следующие — производные от него.</summary>
    private bool TryConvertByExtension(Inline node, InlineState state, List<InlineVm> output)
    {
        var context = new MdConvertContext(this, listDepth: 0, text => Run(text, state));
        foreach (var extension in _extensions)
        {
            var converted = _guard.Call(
                extension,
                () => extension
                    .ConvertInlines(node, context)
                    ?.Select((part, index) => (InlineVm)Owned(StampPart(part, node, index), extension))
                    .ToList(),
                null);

            if (converted is not null)
            {
                output.AddRange(converted);
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
        var linkedState = Linked(state, url, title);
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

    /// <summary>Начертание текста внутри ссылки.</summary>
    private static InlineState Linked(InlineState state, string? url, string? title)
        => state with
        {
            Hyperlink = TryCreateUri(url),
            Href = url,
            LinkTitle = string.IsNullOrEmpty(title) ? null : title
        };

    private void AddText(string text, InlineState state, List<InlineVm> output, Inline node)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        output.Add(Stamp(Run(text, state), node));
    }

    /// <summary>Замена SmartyPants по таблице из настройки разбора; таблица там — HTML-сущностями.</summary>
    private string SmartyPantText(SmartyPant smartyPant)
        => _smartyPants is not null && _smartyPants.TryGetValue(smartyPant.Type, out var replacement)
            ? WebUtility.HtmlDecode(replacement)
            : GetSourceText(smartyPant.Span);

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
            : GetLineAt(EndOf(block));

        return (block.Line, Math.Max(block.Line, end));
    }

    private MdSourceSpan GetLineSpan(int line)
    {
        var start = _lineStarts[line];
        var end = line + 1 < _lineStarts.Count ? _lineStarts[line + 1] : _source.Length;
        return new(start, end - start, line, line);
    }

    /// <summary>Строка из одних пробелов или пустая.</summary>
    private bool IsWhiteSpaceLine(int line) => _source.AsSpan(_lineStarts[line], LineEnd(line) - _lineStarts[line]).IsWhiteSpace();

    /// <summary>Конец строки без перевода строки.</summary>
    private int LineEnd(int line)
    {
        var end = line + 1 < _lineStarts.Count ? _lineStarts[line + 1] : _source.Length;
        while (end > _lineStarts[line] && _source[end - 1] is '\r' or '\n')
        {
            end--;
        }

        return end;
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

    /// <summary>
    /// Кусок соседних блоков: блок Markdig — или тег либо текст HTML-блока <see cref="Owner"/>.
    /// Позиции — в исходнике, конец не включается.
    /// </summary>
    private sealed record Piece(int Start, int End, int StartLine, int EndLine)
    {
        public Block? Block { get; init; }

        /// <summary>Блок уже превращён: расширением или как HTML-блок с незнакомыми тегами.</summary>
        public IReadOnlyList<BlockVm>? Converted { get; init; }

        public HtmlTag? Tag { get; init; }

        public string? Text { get; init; }

        public HtmlBlock? Owner { get; init; }
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