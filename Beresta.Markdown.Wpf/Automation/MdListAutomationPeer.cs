namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation.Peers;
using Presenters;

/// <summary>Список для автоматизации: маркеры и созданное содержимое пунктов.</summary>
public sealed class MdListAutomationPeer(MdListPresenter owner) : FrameworkElementAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    protected override string GetClassNameCore() => nameof(MdListPresenter);
}