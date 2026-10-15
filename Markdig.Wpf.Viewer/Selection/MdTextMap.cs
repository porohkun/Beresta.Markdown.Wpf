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

    private MdTextMap(string text, Dictionary<object, MdTextRange> owners, List<(BlockVm, MdTextRange)> blocks)
    {
        Text = text;
        _owners = owners;
        _blocks = blocks;
    }

    public static MdTextMap Empty { get; } = new(string.Empty, new(ReferenceEqualityComparer.Instance), []);

    /// <summary>Сквозной текст; неделимые элементы — знаком <see cref="ObjectMark"/>.</summary>
    public string Text { get; }

    public int Length => Text.Length;

    /// <summary>Блоки документа, вложенные тоже, с их кусками карты в порядке чтения.</summary>
    public IReadOnlyList<(BlockVm Block, MdTextRange Range)> Blocks => _blocks;

    public static MdTextMap Build(IReadOnlyList<BlockVm> blocks)
    {
        var builder = new Builder();
        builder.AppendBlocks(blocks);
        return new(builder.Text.ToString(), builder.Owners, builder.BlockRanges);
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
                source.WriteText(new MdTextWriter(builder));
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

                case QuoteVm or ListVm or BlankLineVm:
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

    /// <summary>Текст куска для копирования, без знаков неделимых.</summary>
    public string GetText(MdTextRange range)
    {
        var start = Math.Clamp(range.Start, 0, Length);
        var end = Math.Clamp(range.End, start, Length);
        return Text.Substring(start, end - start).Replace(ObjectMark.ToString(), string.Empty);
    }

    internal sealed class Builder
    {
        public StringBuilder Text { get; } = new();

        public Dictionary<object, MdTextRange> Owners { get; } = new(ReferenceEqualityComparer.Instance);

        public List<(BlockVm, MdTextRange)> BlockRanges { get; } = [];

        public void Append(string text) => Text.Append(text);

        public void AppendInlines(IReadOnlyList<InlineVm> inlines)
        {
            var start = Text.Length;
            foreach (var inline in inlines)
            {
                Text.Append(TextOf(inline));
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
                    break;

                case CodeBlockVm code:
                    AppendInlines(code.Runs);
                    break;

                case QuoteVm quote:
                    AppendBlocks(quote.Blocks);
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

                            AppendInlines(table.Rows[r].Cells[c].Inlines);
                        }
                    }

                    break;

                case BlankLineVm:
                    break;

                case IMdTextSource source:
                    source.WriteText(new MdTextWriter(this));
                    break;

                default:
                    Owners[block] = new(Text.Length, 1);
                    Text.Append(ObjectMark);
                    break;
            }

            BlockRanges[index] = (block, new(start, Text.Length - start));
        }
    }
}
