namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation.Peers;
using Presenters;

/// <summary>Список определений для автоматизации: термины и созданное содержимое определений.</summary>
public sealed class MdDefinitionListAutomationPeer(MdDefinitionListPresenter owner) : FrameworkElementAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    protected override string GetClassNameCore() => nameof(MdDefinitionListPresenter);
}
