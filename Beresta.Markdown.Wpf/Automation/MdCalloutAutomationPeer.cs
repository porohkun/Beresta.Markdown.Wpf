namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Presenters;

/// <summary>A callout for UI Automation: a group named by its title; a collapsible one expands and collapses.</summary>
public sealed class MdCalloutAutomationPeer(MdCalloutPresenter owner) : FrameworkElementAutomationPeer(owner), IExpandCollapseProvider
{
    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState
        => owner.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface == PatternInterface.ExpandCollapse && owner.IsFoldable ? this : base.GetPattern(patternInterface);

    /// <inheritdoc/>
    public void Expand() => owner.SetCurrentValue(MdCalloutPresenter.IsExpandedProperty, true);

    /// <inheritdoc/>
    public void Collapse() => owner.SetCurrentValue(MdCalloutPresenter.IsExpandedProperty, false);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MdCalloutPresenter);

    /// <inheritdoc/>
    protected override string GetNameCore()
        => base.GetNameCore() is { Length: > 0 } name ? name : owner.TitleText;
}
