namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation.Peers;
using Presenters;

/// <summary>Nested blocks for UI Automation: a group of their created content.</summary>
public sealed class MdBlocksAutomationPeer(MdBlocksPresenter owner) : FrameworkElementAutomationPeer(owner)
{
    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MdBlocksPresenter);
}