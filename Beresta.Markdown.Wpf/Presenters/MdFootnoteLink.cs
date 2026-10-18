namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Automation;
using ViewModels;

/// <summary>A footnote link: a number to the definition or ↩ back; a click goes the same way as links.</summary>
public sealed class MdFootnoteLink : Control
{
    /// <summary>The model of the footnote link.</summary>
    public static readonly DependencyProperty LinkProperty =
        DependencyProperty.Register(
            nameof(Link),
            typeof(FootnoteLinkVm),
            typeof(MdFootnoteLink),
            new(null));

    private bool _pressed;

    /// <inheritdoc cref="LinkProperty"/>
    public FootnoteLinkVm? Link
    {
        get => (FootnoteLinkVm?)GetValue(LinkProperty);
        set => SetValue(LinkProperty, value);
    }

    /// <summary>Follows the link the same way as links of the text.</summary>
    public void Activate()
    {
        if (Link is { } link)
        {
            MarkdownLink.Activate(this, "#" + link.Target, null);
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _pressed = true;
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new MdFootnoteLinkAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_pressed)
        {
            _pressed = false;
            e.Handled = true;
            Activate();
        }
    }

    /// <inheritdoc/>
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _pressed = false;
    }
}