namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Controls;
using Abstractions;

public class QuotePresenter : Control
{
    public static readonly DependencyProperty BlocksProperty =
        DependencyProperty.Register(
            nameof(Blocks),
            typeof(IReadOnlyList<IMdBlockVm>),
            typeof(QuotePresenter));

    public IReadOnlyList<IMdBlockVm>? Blocks
    {
        get => (IReadOnlyList<IMdBlockVm>?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }
}