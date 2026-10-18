namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Presenters;

/// <summary>Сворачиваемый блок для автоматизации: группа с именем-заголовком, которая разворачивается и сворачивается.</summary>
public sealed class MdDetailsAutomationPeer(MdDetailsPresenter owner) : FrameworkElementAutomationPeer(owner), IExpandCollapseProvider
{
    public ExpandCollapseState ExpandCollapseState
        => owner.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPattern(patternInterface);

    public void Expand() => owner.SetCurrentValue(MdDetailsPresenter.IsExpandedProperty, true);

    public void Collapse() => owner.SetCurrentValue(MdDetailsPresenter.IsExpandedProperty, false);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(MdDetailsPresenter);

    protected override string GetNameCore()
        => base.GetNameCore() is { Length: > 0 } name ? name : owner.TitleText;
}
