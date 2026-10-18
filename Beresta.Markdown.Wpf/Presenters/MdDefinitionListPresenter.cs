namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Automation;

/// <summary>Пункты списка определений; для автоматизации — список из их созданного содержимого.</summary>
public class MdDefinitionListPresenter : ItemsControl
{
    protected override AutomationPeer OnCreateAutomationPeer() => new MdDefinitionListAutomationPeer(this);
}
