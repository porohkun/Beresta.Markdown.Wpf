namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>A table cell: a line of text or, in a grid table, several blocks.</summary>
public sealed record TableCellVm(IReadOnlyList<InlineVm> Inlines)
    : NodeVm
{
    // BYDESIGN: пустой список инлайнов у каждой ячейки свой: карта различает владельцев текста по
    // списку, и общий пустой список отдал бы всем пустым ячейкам кусок последней.
    /// <summary>The line of text of the cell; a cell of blocks has it empty.</summary>
    public IReadOnlyList<InlineVm> Inlines
    {
        get;
        init => field = Own(value);
    } = Own(Inlines);

    /// <summary>The blocks of a grid table cell; a cell of one line of text has them empty.</summary>
    public IReadOnlyList<BlockVm> Blocks { get; init; } = [];

    /// <summary>How many columns the cell takes: a merged cell of a grid table takes more than one.</summary>
    public int ColumnSpan { get; init; } = 1;

    /// <summary>The cell is drawn with blocks, not with a line of text.</summary>
    public bool HasBlocks => Blocks.Count > 0;

    /// <summary>Список инлайнов ячейки; пустой — свой новый.</summary>
    private static IReadOnlyList<InlineVm> Own(IReadOnlyList<InlineVm> inlines) => inlines.Count == 0 ? new List<InlineVm>() : inlines;
}
