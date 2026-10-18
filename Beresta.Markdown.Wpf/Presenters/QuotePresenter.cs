namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Controls;
using ViewModels;

/// <summary>A quote: a frame around nested blocks.</summary>
public class QuotePresenter : Control
{
    /// <summary>The blocks inside the quote.</summary>
    public static readonly DependencyProperty BlocksProperty =
        DependencyProperty.Register(
            nameof(Blocks),
            typeof(IReadOnlyList<BlockVm>),
            typeof(QuotePresenter));

    /// <inheritdoc cref="BlocksProperty"/>
    public IReadOnlyList<BlockVm>? Blocks
    {
        get => (IReadOnlyList<BlockVm>?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }
}