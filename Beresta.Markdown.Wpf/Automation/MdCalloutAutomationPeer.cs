namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Presenters;

/// <summary>Плашка для автоматизации: группа с именем-заголовком; сворачиваемая — разворачивается и сворачивается.</summary>
public sealed class MdCalloutAutomationPeer(MdCalloutPresenter owner) : FrameworkElementAutomationPeer(owner), IExpandCollapseProvider
{
    public ExpandCollapseState ExpandCollapseState
        => owner.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface == PatternInterface.ExpandCollapse && owner.IsFoldable ? this : base.GetPattern(patternInterface);

    public void Expand() => owner.SetCurrentValue(MdCalloutPresenter.IsExpandedProperty, true);

    public void Collapse() => owner.SetCurrentValue(MdCalloutPresenter.IsExpandedProperty, false);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(MdCalloutPresenter);

    protected override string GetNameCore()
        => base.GetNameCore() is { Length: > 0 } name ? name : owner.TitleText;
}
