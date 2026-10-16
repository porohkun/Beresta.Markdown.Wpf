namespace MarkdigWpfViewer.Selection;

using ViewModels;

/// <summary>Запись текста виджета в текстовую карту, по порядку чтения.</summary>
public sealed class MdTextWriter
{
    private readonly Action<string> _text;
    private readonly Action<IReadOnlyList<InlineVm>> _inlines;
    private readonly Action<IReadOnlyList<BlockVm>> _blocks;

    internal MdTextWriter(MdTextMap.Builder builder)
        : this(builder.Append, builder.AppendInlines, builder.AppendBlocks)
    {
    }

    internal MdTextWriter(Action<string> text, Action<IReadOnlyList<InlineVm>> inlines, Action<IReadOnlyList<BlockVm>> blocks)
    {
        _text = text;
        _inlines = inlines;
        _blocks = blocks;
    }

    /// <summary>Текст, которого нет в инлайнах: копируется, но ни один презентер его не подсвечивает.</summary>
    public void Text(string text) => _text(text);

    /// <summary>Инлайны, которые виджет рисует через <c>MdTextBlock</c> с этим же списком.</summary>
    public void Inlines(IReadOnlyList<InlineVm> inlines) => _inlines(inlines);

    /// <summary>Вложенные блоки — так же, как блоки документа.</summary>
    public void Blocks(IReadOnlyList<BlockVm> blocks) => _blocks(blocks);
}