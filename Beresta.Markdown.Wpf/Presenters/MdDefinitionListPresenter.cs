namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Automation;

/// <summary>The items of a definition list; for UI Automation — a list of their created content.</summary>
public class MdDefinitionListPresenter : ItemsControl
{
    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new MdDefinitionListAutomationPeer(this);
}
