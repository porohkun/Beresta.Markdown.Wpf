namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation.Peers;

/// <summary>The viewer for UI Automation: a document whose children are the created blocks, not view models.</summary>
public sealed class MdDocumentAutomationPeer(MarkdownViewer owner) : FrameworkElementAutomationPeer(owner)
{
    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MarkdownViewer);
}