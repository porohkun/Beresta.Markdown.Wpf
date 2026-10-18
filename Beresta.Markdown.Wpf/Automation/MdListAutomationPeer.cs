namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation.Peers;
using Presenters;

/// <summary>A list for UI Automation: markers and the created content of items.</summary>
public sealed class MdListAutomationPeer(MdListPresenter owner) : FrameworkElementAutomationPeer(owner)
{
    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MdListPresenter);
}