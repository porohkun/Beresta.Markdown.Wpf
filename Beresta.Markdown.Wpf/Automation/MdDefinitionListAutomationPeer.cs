namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation.Peers;
using Presenters;

/// <summary>A definition list for UI Automation: terms and the created content of definitions.</summary>
public sealed class MdDefinitionListAutomationPeer(MdDefinitionListPresenter owner) : FrameworkElementAutomationPeer(owner)
{
    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MdDefinitionListPresenter);
}
