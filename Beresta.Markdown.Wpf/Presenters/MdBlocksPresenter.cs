namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Automation;
using Identity;
using ViewModels;

/// <summary>
/// Blocks inside a block or a widget: a quote, a list item, your own container. Drawn by the same
/// chain of templates and virtualized by the same panel as the document.
/// </summary>
public class MdBlocksPresenter : ItemsControl
{
    /// <summary>The nested blocks.</summary>
    public static readonly DependencyProperty BlocksProperty =
        DependencyProperty.Register(
            nameof(Blocks),
            typeof(IReadOnlyList<BlockVm>),
            typeof(MdBlocksPresenter),
            new(null, OnBlocksChanged));

    private readonly ModelCollection<BlockVm> _blocks = [];

    static MdBlocksPresenter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MdBlocksPresenter), new FrameworkPropertyMetadata(typeof(MdBlocksPresenter)));
    }

    /// <summary>Creates an empty presenter.</summary>
    public MdBlocksPresenter()
    {
        ItemsSource = _blocks;
    }

    /// <inheritdoc cref="BlocksProperty"/>
    public IReadOnlyList<BlockVm>? Blocks
    {
        get => (IReadOnlyList<BlockVm>?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new MdBlocksAutomationPeer(this);

    /// <inheritdoc/>
    protected override DependencyObject GetContainerForItemOverride() => new MdBlockItem();

    /// <inheritdoc/>
    protected override bool IsItemItsOwnContainerOverride(object item) => item is MdBlockItem;

    private static void OnBlocksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdBlocksPresenter)d)._blocks.Apply((IReadOnlyList<BlockVm>?)e.NewValue ?? []);
}