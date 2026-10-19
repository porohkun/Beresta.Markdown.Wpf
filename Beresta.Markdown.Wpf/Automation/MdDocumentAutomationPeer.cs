namespace Beresta.Markdown.Wpf.Automation;

using System.Runtime.CompilerServices;
using System.Windows.Automation.Peers;

/// <summary>
/// The viewer for UI Automation: a document whose children are its blocks, created or not, named with
/// their text.
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
            .Select(item => (AutomationPeer)CreateItemAutomationPeer(item))
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
