namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Extensions;
using Selection;

/// <summary>
/// Контейнер блока во вьюере и во вложенных блоках. Неделимый блок — разделитель, виджет без
/// текста — подсвечивается им целиком. Отдаёт своему блоку его состояние виджета.
/// </summary>
public sealed class MdBlockItem : ContentControl
{
    /// <summary>Подсветка поверх блока; <c>null</c> — блок не подсвечен.</summary>
    public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.Register(
            nameof(Highlight),
            typeof(Brush),
            typeof(MdBlockItem),
            new(null));

    private static readonly DependencyProperty LayerProperty =
        MdSelection.LayerProperty.AddOwner(
            typeof(MdBlockItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnLayerChanged));

    private static readonly DependencyProperty StatesProperty =
        MdWidget.StatesProperty.AddOwner(
            typeof(MdBlockItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnStatesChanged));

    static MdBlockItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MdBlockItem), new FrameworkPropertyMetadata(typeof(MdBlockItem)));
    }

    public Brush? Highlight
    {
        get => (Brush?)GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    /// <summary>Кусок карты неделимого блока; у блока с текстом его нет.</summary>
    internal bool TryGetAtomicRange(out MdTextRange range)
    {
        range = default;
        return GetValue(LayerProperty) is MdDecorationLayer layer
               && Content is { } content
               && layer.Map.TryGetRange(content, out range);
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        UpdateHighlight();
        UpdateState();
    }

    private static void OnLayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdBlockItem)d).UpdateHighlight();

    private static void OnStatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdBlockItem)d).UpdateState();

    /// <summary>Своё состояние или пустое — состояние внешнего виджета внутрь блока не проходит.</summary>
    private void UpdateState()
        => MdWidget.SetState(this, (GetValue(StatesProperty) as WidgetStates)?.StateOf(Content));

    private void UpdateHighlight()
    {
        Brush? brush = null;
        if (TryGetAtomicRange(out var range) && GetValue(LayerProperty) is MdDecorationLayer layer)
        {
            foreach (var (_, highlight) in layer.Within(range))
            {
                brush = highlight;
            }
        }

        Highlight = brush;
    }
}
