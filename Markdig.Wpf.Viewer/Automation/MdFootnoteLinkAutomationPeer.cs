namespace MarkdigWpfViewer.Automation;

using System.Globalization;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Presenters;

/// <summary>Ссылка сноски для автоматизации: ссылка, которую можно нажать.</summary>
public sealed class MdFootnoteLinkAutomationPeer(MdFootnoteLink owner) : FrameworkElementAutomationPeer(owner), IInvokeProvider
{
    public override object? GetPattern(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Invoke ? this : base.GetPattern(patternInterface);

    public void Invoke() => ((MdFootnoteLink)Owner).Activate();

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Hyperlink;

    protected override string GetClassNameCore() => nameof(MdFootnoteLink);

    protected override string GetNameCore()
        => ((MdFootnoteLink)Owner).Link is { } link
            ? link.IsBack ? "↩" : link.Order.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

    protected override List<AutomationPeer>? GetChildrenCore() => null;
}
