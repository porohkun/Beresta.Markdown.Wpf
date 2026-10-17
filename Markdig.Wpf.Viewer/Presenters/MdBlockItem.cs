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
    /// <summary>Заливка поверх блока; <c>null</c> — блок не залит.</summary>
    public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.Register(
            nameof(Highlight),
            typeof(Brush),
            typeof(MdBlockItem),
            new(null));

    /// <summary>Черта или рамка пометки поверх блока; <c>null</c> — её нет.</summary>
    public static readonly DependencyProperty HighlightBorderBrushProperty =
        DependencyProperty.Register(
            nameof(HighlightBorderBrush),
            typeof(Brush),
            typeof(MdBlockItem),
            new(null));

    /// <summary>Толщина черты или рамки пометки по краям блока.</summary>
    public static readonly DependencyProperty HighlightBorderThicknessProperty =
        DependencyProperty.Register(
            nameof(HighlightBorderThickness),
            typeof(Thickness),
            typeof(MdBlockItem),
            new(default(Thickness)));

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

    public Brush? HighlightBorderBrush
    {
        get => (Brush?)GetValue(HighlightBorderBrushProperty);
        set => SetValue(HighlightBorderBrushProperty, value);
    }

    public Thickness HighlightBorderThickness
    {
        get => (Thickness)GetValue(HighlightBorderThicknessProperty);
        set => SetValue(HighlightBorderThicknessProperty, value);
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
        var marks = TryGetAtomicRange(out var range) && GetValue(LayerProperty) is MdDecorationLayer layer
            ? layer.Within(range).Select(mark => (mark.Kind, mark.Brush)).ToList()
            : [];

        var (fill, line, thickness) = MarkLook.Of(marks, this);
        Highlight = fill;
        HighlightBorderBrush = line;
        HighlightBorderThickness = thickness;
    }
}
