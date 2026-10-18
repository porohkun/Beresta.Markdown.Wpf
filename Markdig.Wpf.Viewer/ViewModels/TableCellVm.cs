namespace MarkdigWpfViewer.ViewModels;

/// <summary>Ячейка таблицы: строка текста или, в сетке-таблице, несколько блоков.</summary>
public sealed record TableCellVm(IReadOnlyList<InlineVm> Inlines)
    : NodeVm
{
    // BYDESIGN: пустой список инлайнов у каждой ячейки свой: карта различает владельцев текста по
    // списку, и общий пустой список отдал бы всем пустым ячейкам кусок последней.
    /// <summary>Строка текста ячейки; у ячейки из блоков — пусто.</summary>
    public IReadOnlyList<InlineVm> Inlines { get; init; } = Inlines.Count == 0 ? new List<InlineVm>() : Inlines;

    /// <summary>Блоки ячейки сетки-таблицы; у ячейки из одной строки текста — пусто.</summary>
    public IReadOnlyList<BlockVm> Blocks { get; init; } = [];

    /// <summary>Сколько столбцов занимает ячейка: у объединённой ячейки сетки-таблицы — больше одного.</summary>
    public int ColumnSpan { get; init; } = 1;

    /// <summary>Ячейка рисуется блоками, а не строкой текста.</summary>
    public bool HasBlocks => Blocks.Count > 0;
}
