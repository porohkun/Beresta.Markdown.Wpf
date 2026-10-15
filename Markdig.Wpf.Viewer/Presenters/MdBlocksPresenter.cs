namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Controls;
using Identity;
using ViewModels;

/// <summary>
/// Блоки внутри блока или виджета: цитаты, пункта списка, своего контейнера. Рисуются той же
/// цепочкой шаблонов и виртуализируются той же панелью, что и документ.
/// </summary>
public class MdBlocksPresenter : ItemsControl
{
    /// <summary>Вложенные блоки.</summary>
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

    public MdBlocksPresenter()
    {
        ItemsSource = _blocks;
    }

    public IReadOnlyList<BlockVm>? Blocks
    {
        get => (IReadOnlyList<BlockVm>?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    protected override DependencyObject GetContainerForItemOverride() => new MdBlockItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MdBlockItem;

    private static void OnBlocksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdBlocksPresenter)d)._blocks.Apply((IReadOnlyList<BlockVm>?)e.NewValue ?? []);
}
