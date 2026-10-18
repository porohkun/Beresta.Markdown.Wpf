namespace Beresta.Markdown.Wpf.Automation;

using System.Windows.Automation.Peers;

/// <summary>Вьюер для автоматизации — документ: его дети — созданные блоки, а не вьюмодели.</summary>
public sealed class MdDocumentAutomationPeer(MarkdownViewer owner) : FrameworkElementAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Document;

    protected override string GetClassNameCore() => nameof(MarkdownViewer);
}