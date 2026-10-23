namespace Beresta.Markdown.Wpf;

using System.Windows;
using ViewModels;

/// <summary>
/// A control of the document asks to change the source: a pressed task box, a collapsible block
/// that was expanded or collapsed.
/// </summary>
public sealed class MdSourceEditRequestedEventArgs : RoutedEventArgs
{
    /// <summary>A request to change the source on behalf of an element of the model.</summary>
    public MdSourceEditRequestedEventArgs(RoutedEvent routedEvent, object source, MdSourceEdit edit, NodeVm node)
        : base(routedEvent, source)
    {
        Edit = edit;
        Node = node;
    }

    /// <summary>The change; its positions point into the text of the shown document.</summary>
    public MdSourceEdit Edit { get; }

    /// <summary>The element of the model whose control asks.</summary>
    public NodeVm Node { get; }

    /// <inheritdoc/>
    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget) =>
        ((MdSourceEditRequestedEventHandler)genericHandler)(genericTarget, this);
}
