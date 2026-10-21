namespace Beresta.Markdown.Wpf.Automation;

using System.Runtime.CompilerServices;
using System.Windows.Automation.Peers;
using ViewModels;

/// <summary>
/// The viewer for UI Automation: a document whose children are its blocks, created or not, named with
/// their text; the element a derived viewer puts in place of blocks is there with its own node.
/// </summary>
public sealed class MdDocumentAutomationPeer(MarkdownViewer owner) : ItemsControlAutomationPeer(owner)
{
    private readonly ConditionalWeakTable<object, ItemAutomationPeer> _blocks = new();

    /// <inheritdoc/>
    protected override List<AutomationPeer> GetChildrenCore()
    {
        foreach (var realized in (base.GetChildrenCore() ?? []).OfType<ItemAutomationPeer>())
        {
            _blocks.AddOrUpdate(realized.Item, realized);
        }

        return owner.Items
            .Cast<object>()
            .Select(item => item is SourceWindowVm window && UIElementAutomationPeer.CreatePeerForElement(window.Element) is { } own
                ? own
                : CreateItemAutomationPeer(item))
            .ToList();
    }

    /// <inheritdoc/>
    protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        => _blocks.GetValue(item, block => new MdBlockAutomationPeer(block, this));

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MarkdownViewer);
}
