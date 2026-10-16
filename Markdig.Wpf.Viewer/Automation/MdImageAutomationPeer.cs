namespace MarkdigWpfViewer.Automation;

using System.Windows.Automation.Peers;
using Presenters;

/// <summary>Картинка для автоматизации: имя — её alt, если приложение не задало своё; детей нет.</summary>
public sealed class MdImageAutomationPeer(MdImage owner) : FrameworkElementAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

    protected override string GetClassNameCore() => nameof(MdImage);

    protected override string GetNameCore()
        => base.GetNameCore() is { Length: > 0 } name ? name : ((MdImage)Owner).Image?.Alt ?? string.Empty;

    protected override List<AutomationPeer>? GetChildrenCore() => null;
}