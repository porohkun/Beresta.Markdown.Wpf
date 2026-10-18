namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Presenters;

/// <summary>
/// A task box for UI Automation: a disabled check box with the state of the task from the source;
/// for a task with a status the state is indeterminate.
/// </summary>
public sealed class MdTaskMarkAutomationPeer(MdTaskMark owner) : FrameworkElementAutomationPeer(owner), IToggleProvider
{
    /// <inheritdoc/>
    public ToggleState ToggleState
        => owner.Glyph is not null ? ToggleState.Indeterminate : owner.IsChecked ? ToggleState.On : ToggleState.Off;

    /// <inheritdoc/>
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Toggle ? this : base.GetPattern(patternInterface);

    /// <inheritdoc/>
    public void Toggle() => throw new ElementNotEnabledException();

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MdTaskMark);

    /// <inheritdoc/>
    protected override bool IsEnabledCore() => false;
}
