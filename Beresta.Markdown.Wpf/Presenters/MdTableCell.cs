namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Automation;
using ViewModels;

/// <summary>Ячейка таблицы на экране: место в строках и столбцах модели и свой узел UI Automation.</summary>
internal sealed class MdTableCell(MdTablePresenter table, int row, int column, int span, bool isHeader) : ContentPresenter
{
    public MdTablePresenter Table { get; } = table;

    /// <summary>Строка модели таблицы, без строк пустых мест.</summary>
    public int Row { get; } = row;

    public int Column { get; } = column;

    public int Span { get; } = span;

    public bool IsHeader { get; } = isHeader;

    public TableCellVm? Cell => Content as TableCellVm;

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new MdTableCellAutomationPeer(this);
}
