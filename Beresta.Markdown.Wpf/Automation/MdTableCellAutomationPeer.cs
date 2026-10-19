namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Presenters;
using Selection;

/// <summary>Ячейка таблицы для UI Automation: строка, столбец и охват по модели, имя — текст ячейки из карты.</summary>
internal sealed class MdTableCellAutomationPeer(MdTableCell owner)
    : FrameworkElementAutomationPeer(owner), IGridItemProvider, ITableItemProvider
{
    public int Row => owner.Row;

    public int Column => owner.Column;

    public int RowSpan => 1;

    public int ColumnSpan => owner.Span;

    public IRawElementProviderSimple ContainingGrid => ProviderFromPeer(CreatePeerForElement(owner.Table));

    /// <inheritdoc/>
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface is PatternInterface.GridItem or PatternInterface.TableItem ? this : base.GetPattern(patternInterface);

    public IRawElementProviderSimple[] GetColumnHeaderItems()
        => CreatePeerForElement(owner.Table) is MdTableAutomationPeer table ? table.ColumnHeadersOf(owner.Column) : [];

    public IRawElementProviderSimple[] GetRowHeaderItems() => [];

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => owner.IsHeader ? AutomationControlType.HeaderItem : AutomationControlType.DataItem;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MdTableCell);

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        if (owner.Cell is not { } cell || MarkdownViewer.Owning(owner) is not { TextMap: var map })
        {
            return string.Empty;
        }

        MdTextRange? range = cell.HasBlocks
            ? map.RangeOf(cell.Blocks[0]) is { } first && map.RangeOf(cell.Blocks[^1]) is { } last ? MdTextRange.Between(first.Start, last.End) : null
            : map.TryGetRange(cell.Inlines, out var inlines) ? inlines : null;

        return range is { } found ? map.GetText(found).Trim() : string.Empty;
    }
}
