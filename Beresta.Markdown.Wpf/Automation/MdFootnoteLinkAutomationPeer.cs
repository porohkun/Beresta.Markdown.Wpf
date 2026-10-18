namespace Beresta.Markdown.Wpf.Automation;

using System.Globalization;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Presenters;

/// <summary>A footnote link for UI Automation: a link that can be invoked.</summary>
public sealed class MdFootnoteLinkAutomationPeer(MdFootnoteLink owner) : FrameworkElementAutomationPeer(owner), IInvokeProvider
{
    /// <inheritdoc/>
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Invoke ? this : base.GetPattern(patternInterface);

    /// <inheritdoc/>
    public void Invoke() => ((MdFootnoteLink)Owner).Activate();

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Hyperlink;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MdFootnoteLink);

    /// <inheritdoc/>
    protected override string GetNameCore()
        => ((MdFootnoteLink)Owner).Link is { } link
            ? link.IsBack ? "↩" : link.Order.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore() => null;
}