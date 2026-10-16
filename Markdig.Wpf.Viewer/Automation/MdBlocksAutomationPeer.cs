namespace MarkdigWpfViewer.Automation;

using System.Windows.Automation.Peers;
using Presenters;

/// <summary>Вложенные блоки для автоматизации — группа из их созданного содержимого.</summary>
public sealed class MdBlocksAutomationPeer(MdBlocksPresenter owner) : FrameworkElementAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(MdBlocksPresenter);
}
