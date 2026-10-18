namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation.Peers;
using Presenters;

/// <summary>
/// An image for UI Automation: named by its alt text unless the application sets a name; no
/// children.
/// </summary>
public sealed class MdImageAutomationPeer(MdImage owner) : FrameworkElementAutomationPeer(owner)
{
    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MdImage);

    /// <inheritdoc/>
    protected override string GetNameCore()
        => base.GetNameCore() is { Length: > 0 } name ? name : ((MdImage)Owner).Image?.Alt ?? string.Empty;

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore() => null;
}