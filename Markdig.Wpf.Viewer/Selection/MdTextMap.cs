namespace MarkdigWpfViewer.Selection;

using System.Text;
using ViewModels;

/// <summary>
/// Текстовая карта документа: сквозной текст, где у каждого списка инлайнов и неделимого
/// элемента есть свой кусок.
/// </summary>
public sealed class MdTextMap
{
    /// <summary>Знак неделимого элемента в тексте карты; при копировании пропадает.</summary>
    public const char ObjectMark = '￼';

    private readonly Dictionary<object, MdTextRange> _owners;
    private readonly List<(BlockVm Block, MdTextRange Range)> _blocks;
    private readonly Dictionary<int, string> _copies;
    private readonly Dictionary<string, MdTextRange> _anchors;

    private MdTextMap(Builder builder)
    {
        Text = builder.Text.ToString();
        _owners = builder.Owners;
        _blocks = builder.BlockRanges;
        _copies = builder.Copies;
        _anchors = builder.Anchors;
        Headings = builder.Headings;
    }

    public static MdTextMap Empty { get; } = new(new Builder());

    /// <summary>Сквозной текст; неделимые элементы — знаком <see cref="ObjectMark"/>.</summary>
    public string Text { get; }

    public int Length => Text.Length;

    /// <summary>Блоки документа, вложенные тоже, с их кусками карты в порядке чтения.</summary>
    public IReadOnlyList<(BlockVm Block, MdTextRange Range)> Blocks => _blocks;

    /// <summary>Заголовки документа в порядке чтения — для оглавления.</summary>
    public IReadOnlyList<MdHeading> Headings { get; }

    public static MdTextMap Build(IReadOnlyList<BlockVm> blocks)
    {
        var builder = new Builder();
        builder.AppendBlocks(blocks);
        return new(builder);
    }

    /// <summary>Кусок карты якоря — заголовка или сноски — по его идентификатору без <c>#</c>.</summary>
    public bool TryGetAnchor(string id, out MdTextRange range) => _anchors.TryGetValue(id, out range);

    /// <summary>
    /// Якорь заголовка с этим текстом, как у GitHub: строчные буквы, цифры, дефис и подчёркивание,
    /// пробел — дефисом. Повтор того же заголовка в документе получает номер через дефис.
    /// </summary>
    public static string AnchorOf(string text)
    {
        var slug = new StringBuilder(text.Length);
        foreach (var symbol in text.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(symbol) || symbol is '-' or '_')
            {
                slug.Append(symbol);
            }
            else if (char.IsWhiteSpace(symbol))
            {
                slug.Append('-');
            }
        }

        return slug.ToString();
    }

    /// <summary>Текст инлайна в карте: кусок текста, перенос строки или знак неделимого.</summary>
    public static string TextOf(InlineVm inline)
    {
        switch (inline)
        {
            case RunVm run:
                return run.Text;
            case LineBreakVm:
                return "\n";
            case IMdTextSource source:
                var builder = new Builder();
                source.WriteText(new(builder));
                return builder.Text.ToString();
            default:
                return ObjectMark.ToString();
        }
    }

    /// <summary>Кусок карты списка инлайнов или неделимого элемента.</summary>
    public bool TryGetRange(object owner, out MdTextRange range) => _owners.TryGetValue(owner, out range);

    /// <summary>
    /// Кусок карты, который занимают строки исходника с <paramref name="startLine"/> по
    /// <paramref name="endLine"/> включительно; <c>null</c> — на этих строках ничего не видно.
    /// Строки текста и кода узнаются точно, неделимое и виджет — целиком.
    /// </summary>
    public MdTextRange? RangeOfSourceLines(int startLine, int endLine)
    {
        var start = int.MaxValue;
        var end = int.MinValue;

        void Take(int from, int length)
        {
            start = Math.Min(start, from);
            end = Math.Max(end, from + length);
        }

        bool Intersects(MdSourceSpan source)
            => !source.IsEmpty && source.EndLine >= startLine && source.StartLine <= endLine;

        void TakeInlines(IReadOnlyList<InlineVm> inlines)
        {
            if (!TryGetRange(inlines, out var range))
            {
                return;
            }

            var position = range.Start;
            var located = false;
            foreach (var inline in inlines)
            {
                var length = TextOf(inline).Length;
                located |= !inline.Source.IsEmpty;
                if (Intersects(inline.Source))
                {
                    Take(position, length);
                }

                position += length;
            }

            if (!located)
            {
                Take(range.Start, range.Length);
            }
        }

        foreach (var (block, range) in _blocks)
        {
            if (!Intersects(block.Source))
            {
                continue;
            }

            switch (block)
            {
                case ParagraphVm paragraph:
                    TakeInlines(paragraph.Inlines);
                    break;

                case HeadingVm heading:
                    TakeInlines(heading.Inlines);
                    break;

                case FigureCaptionVm caption:
                    TakeInlines(caption.Inlines);
                    break;

                case CalloutVm callout:
                    TakeInlines(callout.Title);
                    break;

                case DetailsVm details:
                    TakeInlines(details.Summary);
                    break;

                case DefinitionListVm definitions:
                    foreach (var item in definitions.Items)
                    {
                        TakeInlines(item.Terms);
                    }

                    break;

                case CodeBlockVm code when TryGetRange(code.Runs, out var codeRange):
                    var lineStart = 0;
                    var lines = code.Code.Split('\n');
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var line = code.CodeFirstLine + i;
                        if (line >= startLine && line <= endLine)
                        {
                            Take(codeRange.Start + lineStart, lines[i].Length);
                        }

                        lineStart += lines[i].Length + 1;
                    }

                    break;

                case TableVm table:
                    foreach (var row in table.Rows.Where(row => Intersects(row.Source)))
                    {
                        foreach (var cell in row.Cells)
                        {
                            TakeInlines(cell.Inlines);
                        }
                    }

                    break;

                case QuoteVm or ListVm or BlankLineVm or FigureVm or FooterVm or AlignedVm:
                    break;

                default:
                    if (!range.IsEmpty)
                    {
                        Take(range.Start, range.Length);
                    }

                    break;
            }
        }

        return end >= start ? new MdTextRange(start, end - start) : null;
    }

    /// <summary>
    /// Строка исходника, на которой стоит позиция карты: по инлайну, в блоке кода — по строке кода;
    /// <c>null</c> — позиции нет ни в одном блоке или строка неизвестна.
    /// </summary>
    public int? SourceLineOf(int position)
    {
        if (Innermost(position, _ => true) is not { Block: var block })
        {
            return null;
        }

        switch (block)
        {
            case ParagraphVm paragraph when LineIn(paragraph.Inlines, position) is { } line:
                return line;

            case HeadingVm heading when LineIn(heading.Inlines, position) is { } line:
                return line;

            case FigureCaptionVm caption when LineIn(caption.Inlines, position) is { } line:
                return line;

            case CalloutVm callout when LineIn(callout.Title, position) is { } line:
                return line;

            case DetailsVm details when LineIn(details.Summary, position) is { } line:
                return line;

            case DefinitionListVm definitions
                when definitions.Items.Select(item => LineIn(item.Terms, position)).FirstOrDefault(line => line is not null) is { } line:
                return line;

            case CodeBlockVm code when TryGetRange(code.Runs, out var codeRange):
                var before = Math.Clamp(position - codeRange.Start, 0, code.Code.Length);
                return code.CodeFirstLine + code.Code.AsSpan(0, before).Count('\n');

            case TableVm table:
                foreach (var row in table.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        if (TryGetRange(cell.Inlines, out var cellRange) && cellRange.Start <= position && position <= cellRange.End)
                        {
                            return LineIn(cell.Inlines, position) ?? (row.Source.IsEmpty ? null : row.Source.StartLine);
                        }
                    }
                }

                break;
        }

        return block.Source.IsEmpty ? null : block.Source.StartLine;
    }

    /// <summary>Место позиции относительно самого глубокого блока с идентификатором — чтобы найти его в карте нового текста.</summary>
    internal (long NodeId, int Offset)? Locate(int position)
        => Innermost(position, block => block.NodeId != 0) is { } found
            ? (found.Block.NodeId, position - found.Range.Start)
            : null;

    /// <summary>Позиция в этой карте для места из карты прошлого текста; блока больше нет — <c>null</c>.</summary>
    internal int? Find((long NodeId, int Offset) place)
    {
        foreach (var (block, range) in _blocks)
        {
            if (block.NodeId == place.NodeId)
            {
                return range.Start + Math.Clamp(place.Offset, 0, range.Length);
            }
        }

        return null;
    }

    /// <summary>Текст куска для копирования: неделимые — своим текстом для копирования или ничем.</summary>
    public string GetText(MdTextRange range)
    {
        var start = Math.Clamp(range.Start, 0, Length);
        var end = Math.Clamp(range.End, start, Length);
        var text = new StringBuilder(end - start);

        for (var position = start; position < end;)
        {
            var mark = Text.IndexOf(ObjectMark, position, end - position);
            if (mark < 0)
            {
                text.Append(Text, position, end - position);
                break;
            }

            text.Append(Text, position, mark - position);
            if (_copies.TryGetValue(mark, out var copy))
            {
                text.Append(copy);
            }

            position = mark + 1;
        }

        return text.ToString();
    }

    /// <summary>
    /// Самый глубокий подходящий блок, в куске которого стоит позиция, считая его конец; вложенные
    /// блоки идут в списке после своих контейнеров.
    /// </summary>
    private (BlockVm Block, MdTextRange Range)? Innermost(int position, Func<BlockVm, bool> suits)
    {
        for (var i = _blocks.Count - 1; i >= 0; i--)
        {
            var (block, range) = _blocks[i];
            if (!range.IsEmpty && range.Start <= position && position <= range.End && suits(block))
            {
                return (block, range);
            }
        }

        return null;
    }

    /// <summary>Строка исходника инлайна, на котором стоит позиция; позиция вне этих инлайнов — <c>null</c>.</summary>
    private int? LineIn(IReadOnlyList<InlineVm> inlines, int position)
    {
        if (!TryGetRange(inlines, out var range) || position < range.Start || position > range.End)
        {
            return null;
        }

        var start = range.Start;
        for (var i = 0; i < inlines.Count; i++)
        {
            var end = start + TextOf(inlines[i]).Length;
            if (position < end || i == inlines.Count - 1)
            {
                return inlines[i].Source.IsEmpty ? null : inlines[i].Source.StartLine;
            }

            start = end;
        }

        return null;
    }

    internal sealed class Builder
    {
        private readonly Dictionary<string, int> _slugs = [];

        public StringBuilder Text { get; } = new();

        public Dictionary<object, MdTextRange> Owners { get; } = new(ReferenceEqualityComparer.Instance);

        public List<(BlockVm, MdTextRange)> BlockRanges { get; } = [];

        /// <summary>Тексты для копирования неделимых по позициям их знаков.</summary>
        public Dictionary<int, string> Copies { get; } = [];

        public Dictionary<string, MdTextRange> Anchors { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<MdHeading> Headings { get; } = [];

        public void Append(string text) => Text.Append(text);

        public void AppendInlines(IReadOnlyList<InlineVm> inlines)
        {
            var start = Text.Length;
            foreach (var inline in inlines)
            {
                var text = TextOf(inline);
                if (inline is IMdCopyText copy && text is [ObjectMark])
                {
                    Copies[Text.Length] = copy.CopyText;
                }

                if (inline is FootnoteLinkVm { IsBack: false } reference)
                {
                    Anchors.TryAdd($"fnref:{reference.Index}", new(Text.Length, text.Length));
                }

                Text.Append(text);
            }

            Owners[inlines] = new(start, Text.Length - start);
        }

        public void AppendBlocks(IReadOnlyList<BlockVm> blocks)
        {
            for (var i = 0; i < blocks.Count; i++)
            {
                if (i > 0)
                {
                    Text.Append('\n');
                }

                AppendBlock(blocks[i]);
            }
        }

        private void AppendBlock(BlockVm block)
        {
            var start = Text.Length;
            var index = BlockRanges.Count;
            BlockRanges.Add((block, default));

            switch (block)
            {
                case ParagraphVm paragraph:
                    AppendInlines(paragraph.Inlines);
                    break;

                case HeadingVm heading:
                    AppendInlines(heading.Inlines);
                    AddHeading(heading);
                    break;

                case CodeBlockVm code:
                    AppendInlines(code.Runs);
                    break;

                case QuoteVm quote:
                    AppendBlocks(quote.Blocks);
                    break;

                case FigureVm figure:
                    AppendBlocks(figure.Blocks);
                    break;

                case FigureCaptionVm caption:
                    AppendInlines(caption.Inlines);
                    break;

                case FooterVm footer:
                    AppendBlocks(footer.Blocks);
                    break;

                case AlignedVm aligned:
                    AppendBlocks(aligned.Blocks);
                    break;

                case CalloutVm callout:
                    AppendTitled(callout.Title, callout.Blocks);
                    break;

                case DetailsVm details:
                    AppendTitled(details.Summary, details.Blocks);
                    break;

                case DefinitionListVm definitions:
                    for (var i = 0; i < definitions.Items.Count; i++)
                    {
                        if (i > 0)
                        {
                            Text.Append('\n');
                        }

                        AppendTitled(definitions.Items[i].Terms, definitions.Items[i].Blocks);
                    }

                    break;

                case FootnoteVm footnote:
                    AppendBlocks(footnote.Blocks);
                    Anchors.TryAdd($"fn:{footnote.Order}", new(start, Text.Length - start));
                    break;

                case ListVm list:
                    for (var i = 0; i < list.Items.Count; i++)
                    {
                        if (i > 0)
                        {
                            Text.Append('\n');
                        }

                        AppendBlocks(list.Items[i].Blocks);
                    }

                    break;

                case TableVm table:
                    for (var r = 0; r < table.Rows.Count; r++)
                    {
                        if (r > 0)
                        {
                            Text.Append('\n');
                        }

                        for (var c = 0; c < table.Rows[r].Cells.Count; c++)
                        {
                            if (c > 0)
                            {
                                Text.Append('\t');
                            }

                            var cell = table.Rows[r].Cells[c];
                            if (cell.HasBlocks)
                            {
                                AppendBlocks(cell.Blocks);
                            }
                            else
                            {
                                AppendInlines(cell.Inlines);
                            }
                        }
                    }

                    break;

                case BlankLineVm:
                    break;

                case IMdTextSource source:
                    source.WriteText(new(this));
                    break;

                default:
                    if (block is IMdCopyText copy)
                    {
                        Copies[Text.Length] = copy.CopyText;
                    }

                    Owners[block] = new(Text.Length, 1);
                    Text.Append(ObjectMark);
                    break;
            }

            BlockRanges[index] = (block, new(start, Text.Length - start));
        }

        /// <summary>Заголовок блока, если он есть, строкой перед вложенными блоками.</summary>
        private void AppendTitled(IReadOnlyList<InlineVm> title, IReadOnlyList<BlockVm> blocks)
        {
            if (title.Count > 0)
            {
                AppendInlines(title);
                if (blocks.Count > 0)
                {
                    Text.Append('\n');
                }
            }

            AppendBlocks(blocks);
        }

        /// <summary>Заголовок — в оглавление, его якоря — свой, как у GitHub, и идентификатор от Markdig.</summary>
        private void AddHeading(HeadingVm heading)
        {
            var range = Owners[heading.Inlines];
            var text = Text.ToString(range.Start, range.Length).Replace(ObjectMark.ToString(), string.Empty).Trim();

            var slug = AnchorOf(text);
            if (_slugs.TryGetValue(slug, out var seen))
            {
                _slugs[slug] = seen + 1;
                slug = $"{slug}-{seen + 1}";
            }
            else
            {
                _slugs[slug] = 0;
            }

            if (slug.Length > 0)
            {
                Anchors.TryAdd(slug, range);
            }

            if (heading.Id is { Length: > 0 } id)
            {
                Anchors.TryAdd(id, range);
            }

            Headings.Add(new(heading.Level, text, slug, range, heading.Source.StartLine));
        }
    }
}