namespace MarkdigWpfViewer.Selection;

using ViewModels;

/// <summary>Запись текста виджета в текстовую карту, по порядку чтения.</summary>
public sealed class MdTextWriter
{
    private readonly MdTextMap.Builder _builder;

    internal MdTextWriter(MdTextMap.Builder builder)
    {
        _builder = builder;
    }

    /// <summary>Текст, которого нет в инлайнах: копируется, но ни один презентер его не подсвечивает.</summary>
    public void Text(string text) => _builder.Append(text);

    /// <summary>Инлайны, которые виджет рисует через <c>MdTextBlock</c> с этим же списком.</summary>
    public void Inlines(IReadOnlyList<InlineVm> inlines) => _builder.AppendInlines(inlines);

    /// <summary>Вложенные блоки — так же, как блоки документа.</summary>
    public void Blocks(IReadOnlyList<BlockVm> blocks) => _builder.AppendBlocks(blocks);
}
