namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Controls;
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

    static MdBlocksPresenter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MdBlocksPresenter), new FrameworkPropertyMetadata(typeof(MdBlocksPresenter)));
    }

    public IReadOnlyList<BlockVm>? Blocks
    {
        get => (IReadOnlyList<BlockVm>?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    private static void OnBlocksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdBlocksPresenter)d).ItemsSource = (IReadOnlyList<BlockVm>?)e.NewValue;
}
