namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Presenters;

/// <summary>
/// A task box for UI Automation: a check box with the state of the task from the source, enabled
/// while the controls of the viewer edit the source; for a task with a status the state is
/// indeterminate.
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
    public void Toggle()
    {
        if (!owner.Press())
        {
            throw new ElementNotEnabledException();
        }
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MdTaskMark);

    /// <inheritdoc/>
    protected override bool IsEnabledCore() => owner.IsEditable;
}
