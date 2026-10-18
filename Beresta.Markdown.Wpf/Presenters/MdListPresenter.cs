namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Automation;

/// <summary>Пункты списка; для автоматизации — список из их созданного содержимого.</summary>
public class MdListPresenter : ItemsControl
{
    protected override AutomationPeer OnCreateAutomationPeer() => new MdListAutomationPeer(this);
}