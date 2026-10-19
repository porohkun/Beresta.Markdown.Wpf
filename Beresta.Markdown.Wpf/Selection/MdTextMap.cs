namespace Beresta.Markdown.Wpf.Selection;

using System.Text;
using Extensions;
using ViewModels;

/// <summary>
/// The text map of the document: continuous text where every list of inlines and every indivisible
/// element has its own piece.
/// </summary>
public sealed class MdTextMap
{
    /// <summary>The character of an indivisible element in the text of the map; it drops out on copying.</summary>
    public const char ObjectMark = '￼';

    private readonly Dictionary<object, MdTextRange> _owners;
    private readonly List<(BlockVm Block, MdTextRange Range)> _blocks;
    private readonly Dictionary<int, string> _copies;
    private readonly Dictionary<string, MdTextRange> _anchors;
    private readonly SourceLines? _lines;
    private Dictionary<long, (BlockVm Block, MdTextRange Range)>? _byId;
    private LinesIndex? _byLine;

    private MdTextMap(Builder builder, string? source)
    {
        Text = builder.Text.ToString();
        _owners = builder.Owners;
        _blocks = builder.BlockRanges;
        _copies = builder.Copies;
        _anchors = builder.Anchors;
        _lines = source is null ? null : new(source);
        Headings = builder.Headings;
    }

    /// <summary>The map of an empty document.</summary>
    public static MdTextMap Empty { get; } = new(new(ExtensionGuard.Silent), null);

    /// <summary>Continuous text; indivisible elements are the character <see cref="ObjectMark"/>.</summary>
    public string Text { get; }

    /// <summary>The length of the text of the map.</summary>
    public int Length => Text.Length;

    /// <summary>Blocks of the document, nested ones too, with their pieces of the map in reading order.</summary>
    public IReadOnlyList<(BlockVm Block, MdTextRange Range)> Blocks => _blocks;

    /// <summary>Headings of the document in reading order — for a table of contents.</summary>
    public IReadOnlyList<MdHeading> Headings { get; }

    /// <summary>The map of blocks without the source text.</summary>
    public static MdTextMap Build(IReadOnlyList<BlockVm> blocks)
    {
        var builder = new Builder(ExtensionGuard.Silent);
        builder.AppendBlocks(blocks);
        return new(builder, null);
    }

    /// <summary>
    /// The map of blocks together with the source text: tracked lines recognize themselves by the
    /// text of the lines.
    /// </summary>
    public static MdTextMap Build(IReadOnlyList<BlockVm> blocks, string source) => Build(blocks, source, ExtensionGuard.Silent);

    /// <summary>Карта блоков с текстом исходника; код виджетов идёт через <paramref name="guard"/>.</summary>
    internal static MdTextMap Build(IReadOnlyList<BlockVm> blocks, string source, ExtensionGuard guard)
    {
        var builder = new Builder(guard);
        builder.AppendBlocks(blocks);
        return new(builder, source);
    }

    /// <summary>
    /// The anchor of a heading with this text, as on GitHub: lowercase letters, digits, hyphens and
    /// underscores, a space becomes a hyphen. A repeat of the same heading in the document gets a
    /// number after a hyphen.
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

    /// <summary>
    /// The text of an inline in the map: a piece of text, a line break or the character of an
    /// indivisible element.
    /// </summary>
    public static string TextOf(InlineVm inline) => TextOf(inline, ExtensionGuard.Silent);

    /// <summary>Текст инлайна в карте; код виджета идёт через <paramref name="guard"/>, упал — виджет неделимый.</summary>
    internal static string TextOf(InlineVm inline, ExtensionGuard guard)
    {
        switch (inline)
        {
            case RunVm run:
                return run.Text;
            case LineBreakVm:
                return "\n";
            case IMdTextSource source:
                var builder = new Builder(guard);
                return guard.CallWidget(inline, () => source.WriteText(new(builder)))
                    ? builder.Text.ToString()
                    : ObjectMark.ToString();
            default:
                return ObjectMark.ToString();
        }
    }

    /// <summary>
    /// The piece of the map of an anchor — a heading or a footnote — by its identifier without
    /// <c>#</c>.
    /// </summary>
    public bool TryGetAnchor(string id, out MdTextRange range) => _anchors.TryGetValue(id, out range);

    /// <summary>The piece of the map of a list of inlines or of an indivisible element.</summary>
    public bool TryGetRange(object owner, out MdTextRange range) => _owners.TryGetValue(owner, out range);

    /// <summary>
    /// The piece of the map taken by source lines from <paramref name="startLine"/> to
    /// <paramref name="endLine"/> inclusive; <c>null</c> — nothing is visible on these lines. Lines
    /// of text and code are recognized exactly, an indivisible element and a widget — as a whole.
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

        var index = _byLine ??= LinesIndex.Of(_blocks);
        var to = FirstWhere(index.Starts.Length, k => index.Starts[k] > endLine);
        for (var k = FirstWhere(to, k => index.Reach[k] >= startLine); k < to; k++)
        {
            var (block, range) = _blocks[index.Order[k]];
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
    /// The source line a position of the map stands on: by the inline, in a code block — by the
    /// line of code; <c>null</c> — the position is in no block or the line is unknown.
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
                return code.CodeFirstLine + CharCount.In(code.Code.AsSpan(0, before), '\n');

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

    /// <summary>The text of a piece for copying: indivisible elements give their text for copying or nothing.</summary>
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

    /// <summary>Remembers a piece of this map so that it can be found in the map of new text.</summary>
    public MdTrackedRange Track(MdTextRange range)
    {
        var start = Math.Clamp(range.Start, 0, Length);
        var end = Math.Clamp(range.End, start, Length);
        return new(EndAt(start, start, end, isStart: true), EndAt(end, start, end, isStart: false));
    }

    /// <summary>
    /// The place of a tracked piece in this map. Both ends found — the piece between them; one found —
    /// an empty piece at its place; none found or the block of an end is deleted — <c>null</c>: the text
    /// of the piece is gone.
    /// </summary>
    public MdTextRange? Find(MdTrackedRange tracked)
    {
        if (tracked.Start is not { } first || tracked.End is not { } last
            || BlockOf(first.NodeId) is not { } startBlock || BlockOf(last.NodeId) is not { } endBlock)
        {
            return null;
        }

        var start = startBlock.Range.Start + TextTracking.Place(Text, startBlock.Range, first);
        var end = endBlock.Range.Start + TextTracking.Place(Text, endBlock.Range, last);
        return (start, end) switch
        {
            ({ } from, { } to) => from <= to ? new MdTextRange(from, to - from) : new MdTextRange(to, 0),
            ({ } from, null) => new MdTextRange(from, 0),
            (null, { } to) => new MdTextRange(to, 0),
            _ => null
        };
    }

    /// <summary>
    /// Remembers source lines from <paramref name="startLine"/> to <paramref name="endLine"/>
    /// inclusive.
    /// </summary>
    public MdTrackedLines TrackSourceLines(int startLine, int endLine)
        => new(LineEndAt(Math.Min(startLine, endLine)), LineEndAt(Math.Max(startLine, endLine)));

    /// <summary>
    /// The lines of a tracked range in this text; the block where its edge stands is deleted —
    /// <c>null</c>. The lines up to the end of the range are deleted — the first one stays.
    /// </summary>
    public (int StartLine, int EndLine)? Find(MdTrackedLines tracked)
    {
        if (tracked.Start is not { } first || tracked.End is not { } last
            || BlockOf(first.NodeId) is not { } startBlock || BlockOf(last.NodeId) is not { } endBlock)
        {
            return null;
        }

        var start = PlaceLine(first, startBlock.Block.Source);
        var end = PlaceLine(last, endBlock.Block.Source);
        return (start, Math.Max(start, end));
    }

    /// <summary>
    /// Самый глубокий подходящий блок, в куске которого стоит позиция, считая его конец; вложенные
    /// блоки идут в списке после своих контейнеров.
    /// </summary>
    private (BlockVm Block, MdTextRange Range)? Innermost(int position, Func<BlockVm, bool> suits)
    {
        for (var i = FirstWhere(_blocks.Count, i => _blocks[i].Range.Start > position) - 1; i >= 0; i--)
        {
            var (block, range) = _blocks[i];
            if (!range.IsEmpty && position <= range.End && suits(block))
            {
                return (block, range);
            }
        }

        return null;
    }

    /// <summary>Первый номер до <paramref name="count"/>, с которого условие выполняется для всех следующих; такого нет — <paramref name="count"/>.</summary>
    private static int FirstWhere(int count, Func<int, bool> holds)
    {
        var low = 0;
        var high = count;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (holds(middle))
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        return low;
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

    /// <summary>Кусок карты блока; блока в карте нет — <c>null</c>.</summary>
    internal MdTextRange? RangeOf(BlockVm block)
    {
        if (block.NodeId != 0 && BlockOf(block.NodeId) is { } known && ReferenceEquals(known.Block, block))
        {
            return known.Range;
        }

        return _blocks.FirstOrDefault(entry => ReferenceEquals(entry.Block, block)) is { Block: not null } entry ? entry.Range : null;
    }

    private (BlockVm Block, MdTextRange Range)? BlockOf(long nodeId)
    {
        if (_byId is null)
        {
            _byId = [];
            foreach (var entry in _blocks.Where(entry => entry.Block.NodeId != 0))
            {
                _byId.TryAdd(entry.Block.NodeId, entry);
            }
        }

        return _byId.TryGetValue(nodeId, out var found) ? found : null;
    }

    /// <summary>Конец куска <paramref name="from"/>–<paramref name="to"/> в позиции <paramref name="position"/>.</summary>
    private TrackedEnd? EndAt(int position, int from, int to, bool isStart)
    {
        if (Innermost(position, block => block.NodeId != 0) is not { } found)
        {
            return null;
        }

        var (block, range) = found;
        var before = Math.Max(range.Start, position - TextTracking.Context);
        var after = Math.Min(range.End, position + TextTracking.Context);
        if (isStart)
        {
            after = Math.Min(after, to);
        }
        else
        {
            before = Math.Max(before, from);
        }

        return new(block.NodeId, position - range.Start, Text[before..position], Text[position..after], isStart);
    }

    /// <summary>Край отслеживаемых строк: самый глубокий блок со строкой, а вне блоков — ближайший блок.</summary>
    private TrackedLine? LineEndAt(int line)
    {
        BlockVm? inner = null;
        BlockVm? next = null;
        BlockVm? previous = null;

        foreach (var (block, _) in _blocks)
        {
            var source = block.Source;
            if (block.NodeId == 0 || source.IsEmpty)
            {
                continue;
            }

            if (source.StartLine <= line && line <= source.EndLine)
            {
                inner = block;
            }
            else if (source.StartLine > line)
            {
                if (next is null || source.StartLine < next.Source.StartLine)
                {
                    next = block;
                }
            }
            else if (previous is null || source.EndLine > previous.Source.EndLine)
            {
                previous = block;
            }
        }

        var (owner, delta, fromEnd) = inner is not null ? (inner, line - inner.Source.StartLine, false)
            : next is not null ? (next, line - next.Source.StartLine, false)
            : previous is not null ? (previous, line - previous.Source.EndLine, true)
            : default;

        return owner is null
            ? null
            : new(
                owner.NodeId,
                delta,
                Inside: inner is not null,
                fromEnd,
                _lines?.TextOf(line),
                _lines?.TextOf(line - 1),
                _lines?.TextOf(line + 1));
    }

    /// <summary>
    /// Строка края в новом тексте: внутри блока — среди его строк, вне блоков — среди строк до
    /// следующего блока или после предыдущего. Ищется строка с тем же текстом; не нашлась —
    /// прежний сдвиг от блока.
    /// </summary>
    private int PlaceLine(TrackedLine line, MdSourceSpan source)
    {
        if (line.Inside)
        {
            var inside = Math.Clamp(source.StartLine + line.Delta, source.StartLine, source.EndLine);
            return SameLine(line, source.StartLine, source.EndLine, inside) ?? inside;
        }

        var candidate = Math.Max(0, (line.FromEnd ? source.EndLine : source.StartLine) + line.Delta);
        var (from, to) = line.FromEnd ? (source.EndLine + 1, int.MaxValue) : (0, source.StartLine - 1);
        return SameLine(line, from, to, candidate) ?? candidate;
    }

    /// <summary>
    /// Строка с текстом края между <paramref name="from"/> и <paramref name="to"/>: с теми же соседями
    /// охотнее, из равных ближайшая к <paramref name="near"/>; нет такой — <c>null</c>.
    /// </summary>
    private int? SameLine(TrackedLine line, int from, int to, int near)
    {
        if (line.Text is null || _lines is null)
        {
            return null;
        }

        int? best = null;
        var bestScore = -1;
        for (var i = Math.Max(0, from); i <= Math.Min(to, _lines.Count - 1); i++)
        {
            if (!_lines.Is(i, line.Text))
            {
                continue;
            }

            var score = (_lines.Is(i - 1, line.Previous) ? 1 : 0) + (_lines.Is(i + 1, line.Next) ? 1 : 0);
            if (score > bestScore || (score == bestScore && Math.Abs(i - near) < Math.Abs(best!.Value - near)))
            {
                best = i;
                bestScore = score;
            }
        }

        return best;
    }

    /// <summary>
    /// Блоки с местом в исходнике по первой строке и самая дальняя последняя строка среди них до каждого:
    /// блоки на строках находятся бинарным поиском.
    /// </summary>
    private sealed record LinesIndex(int[] Order, int[] Starts, int[] Reach)
    {
        public static LinesIndex Of(List<(BlockVm Block, MdTextRange Range)> blocks)
        {
            var order = Enumerable.Range(0, blocks.Count)
                .Where(i => !blocks[i].Block.Source.IsEmpty)
                .OrderBy(i => blocks[i].Block.Source.StartLine)
                .ToArray();
            var reach = new int[order.Length];
            var farthest = int.MinValue;
            for (var k = 0; k < order.Length; k++)
            {
                farthest = Math.Max(farthest, blocks[order[k]].Block.Source.EndLine);
                reach[k] = farthest;
            }

            return new(order, order.Select(i => blocks[i].Block.Source.StartLine).ToArray(), reach);
        }
    }

    internal sealed class Builder(ExtensionGuard guard)
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
                var text = TextOf(inline, guard);
                if (inline is IMdCopyText && text is [ObjectMark])
                {
                    AddCopy(inline);
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
                    var writes = new List<Action>();
                    var writer = new MdTextWriter(
                        text => writes.Add(() => Append(text)),
                        inlines => writes.Add(() => AppendInlines(inlines)),
                        blocks => writes.Add(() => AppendBlocks(blocks)));

                    if (guard.CallWidget(block, () => source.WriteText(writer)))
                    {
                        writes.ForEach(write => write());
                    }
                    else
                    {
                        AppendIndivisible(block);
                    }

                    break;

                default:
                    AppendIndivisible(block);
                    break;
            }

            BlockRanges[index] = (block, new(start, Text.Length - start));
        }

        /// <summary>Неделимый блок — одним знаком <see cref="ObjectMark"/>.</summary>
        private void AppendIndivisible(BlockVm block)
        {
            if (block is IMdCopyText)
            {
                AddCopy(block);
            }

            Owners[block] = new(Text.Length, 1);
            Text.Append(ObjectMark);
        }

        /// <summary>Текст для копирования неделимого, чей знак встаёт следующим.</summary>
        private void AddCopy(object widget)
        {
            if (guard.CallWidget(widget, () => ((IMdCopyText)widget).CopyText, null) is { } copy)
            {
                Copies[Text.Length] = copy;
            }
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