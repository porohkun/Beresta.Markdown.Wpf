namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Presenters;

/// <summary>
/// Таблица для UI Automation: сетка строк и столбцов модели без строк пустых мест; заголовки
/// столбцов — ячейки строк заголовка.
/// </summary>
internal sealed class MdTableAutomationPeer(MdTablePresenter owner)
    : FrameworkElementAutomationPeer(owner), IGridProvider, ITableProvider
{
    public int RowCount => owner.Table?.Rows.Count ?? 0;

    public int ColumnCount => owner.Table?.ColumnCount ?? 0;

    public RowOrColumnMajor RowOrColumnMajor => RowOrColumnMajor.RowMajor;

    /// <inheritdoc/>
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface is PatternInterface.Grid or PatternInterface.Table ? this : base.GetPattern(patternInterface);

    /// <summary>Ячейка, которая накрывает строку и столбец модели.</summary>
    public IRawElementProviderSimple GetItem(int row, int column)
    {
        if (row < 0 || row >= RowCount || column < 0 || column >= ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        return owner.Cells.FirstOrDefault(cell => cell.Row == row && Covers(cell, column)) is { } found
            ? ProviderOf(found)
            : throw new ElementNotAvailableException();
    }

    public IRawElementProviderSimple[] GetColumnHeaders()
        => owner.Cells
            .Where(cell => cell.IsHeader)
            .Select(ProviderOf)
            .ToArray();

    public IRawElementProviderSimple[] GetRowHeaders() => [];

    /// <summary>Заголовки столбца — ячейки строк заголовка, которые его накрывают.</summary>
    internal IRawElementProviderSimple[] ColumnHeadersOf(int column)
        => owner.Cells
            .Where(cell => cell.IsHeader && Covers(cell, column))
            .Select(ProviderOf)
            .ToArray();

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Table;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MdTablePresenter);

    private static bool Covers(MdTableCell cell, int column) => column >= cell.Column && column < cell.Column + cell.Span;

    private IRawElementProviderSimple ProviderOf(MdTableCell cell) => ProviderFromPeer(CreatePeerForElement(cell));
}
