namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Automation;
using ViewModels;

/// <summary>Ссылка сноски: номер к определению или ↩ обратно; нажатие идёт тем же путём, что у ссылок.</summary>
public sealed class MdFootnoteLink : Control
{
    public static readonly DependencyProperty LinkProperty =
        DependencyProperty.Register(
            nameof(Link),
            typeof(FootnoteLinkVm),
            typeof(MdFootnoteLink),
            new(null));

    private bool _pressed;

    public FootnoteLinkVm? Link
    {
        get => (FootnoteLinkVm?)GetValue(LinkProperty);
        set => SetValue(LinkProperty, value);
    }

    /// <summary>Переходит по ссылке тем же путём, что и ссылки текста.</summary>
    public void Activate()
    {
        if (Link is { } link)
        {
            MarkdownLink.Activate(this, "#" + link.Target, null);
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _pressed = true;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MdFootnoteLinkAutomationPeer(this);

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

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _pressed = false;
    }
}