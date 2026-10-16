namespace MarkdigWpfViewer.ViewModels;

/// <summary>Ячейка таблицы: строка текста или, в сетке-таблице, несколько блоков.</summary>
public sealed record TableCellVm(IReadOnlyList<InlineVm> Inlines)
    : NodeVm
{
    /// <summary>Блоки ячейки сетки-таблицы; у ячейки из одной строки текста — пусто.</summary>
    public IReadOnlyList<BlockVm> Blocks { get; init; } = [];

    /// <summary>Ячейка рисуется блоками, а не строкой текста.</summary>
    public bool HasBlocks => Blocks.Count > 0;
}