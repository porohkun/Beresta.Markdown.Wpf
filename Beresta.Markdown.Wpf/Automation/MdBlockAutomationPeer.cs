namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using ViewModels;

/// <summary>
/// Блок документа для UI Automation, созданный или нет: имя — его текст из текстовой карты, вид — по
/// модели. Несозданный блок создаётся по <see cref="IVirtualizedItemProvider.Realize"/>.
/// </summary>
internal sealed class MdBlockAutomationPeer(object item, MdDocumentAutomationPeer document)
    : ItemAutomationPeer(item, document), IVirtualizedItemProvider
{
    private MarkdownViewer Viewer => (MarkdownViewer)ItemsControlAutomationPeer.Owner;

    /// <summary>Прокручивает к блоку и доводит раскладку: блок создаётся.</summary>
    void IVirtualizedItemProvider.Realize()
    {
        if (Viewer.BlockPanel is { } panel && Viewer.Items.IndexOf(Item) is var index and >= 0)
        {
            panel.BringIndexIntoViewPublic(index);
            Viewer.UpdateLayout();
        }
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => Item switch
        {
            ListVm or DefinitionListVm => AutomationControlType.List,
            TableVm => AutomationControlType.Table,
            QuoteVm or CalloutVm or DetailsVm or FootnoteVm or FigureVm or FooterVm or AlignedVm => AutomationControlType.Group,
            _ => AutomationControlType.Text
        };

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "MdBlock";

    /// <inheritdoc/>
    protected override string GetNameCore()
        => Item is BlockVm block && Viewer.TextMap.RangeOf(block) is { } range
            ? Viewer.TextMap.GetText(range).Trim()
            : string.Empty;

    /// <inheritdoc/>
    protected override AutomationHeadingLevel GetHeadingLevelCore()
        => Item is HeadingVm heading
            ? AutomationHeadingLevel.Level1 + Math.Clamp(heading.Level, 1, 9) - 1
            : AutomationHeadingLevel.None;

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => Item is not BlankLineVm;

    /// <inheritdoc/>
    protected override bool IsControlElementCore() => Item is not BlankLineVm;
}
