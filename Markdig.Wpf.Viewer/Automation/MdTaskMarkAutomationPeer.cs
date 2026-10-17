namespace MarkdigWpfViewer.Automation;

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Presenters;

/// <summary>Флажок задачи для автоматизации: недоступный флажок с состоянием задачи из исходника.</summary>
public sealed class MdTaskMarkAutomationPeer(MdTaskMark owner) : FrameworkElementAutomationPeer(owner), IToggleProvider
{
    public ToggleState ToggleState => owner.IsChecked ? ToggleState.On : ToggleState.Off;

    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Toggle ? this : base.GetPattern(patternInterface);

    public void Toggle() => throw new ElementNotEnabledException();

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;

    protected override string GetClassNameCore() => nameof(MdTaskMark);

    protected override bool IsEnabledCore() => false;
}
