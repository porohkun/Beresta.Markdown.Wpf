namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Extensions;
using Selection;

/// <summary>
/// The container of a block in the viewer and in nested blocks. An indivisible block — a rule, a
/// widget without text — is highlighted by it as a whole. Gives its block its widget state.
/// </summary>
public sealed class MdBlockItem : ContentControl
{
    /// <summary>A fill over the block; <c>null</c> — the block is not filled.</summary>
    public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.Register(
            nameof(Highlight),
            typeof(Brush),
            typeof(MdBlockItem),
            new(null));

    /// <summary>A line or frame of a mark over the block; <c>null</c> — none.</summary>
    public static readonly DependencyProperty HighlightBorderBrushProperty =
        DependencyProperty.Register(
            nameof(HighlightBorderBrush),
            typeof(Brush),
            typeof(MdBlockItem),
            new(null));

    /// <summary>The thickness of the line or frame of a mark at the edges of the block.</summary>
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

    /// <inheritdoc cref="HighlightProperty"/>
    public Brush? Highlight
    {
        get => (Brush?)GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    /// <inheritdoc cref="HighlightBorderBrushProperty"/>
    public Brush? HighlightBorderBrush
    {
        get => (Brush?)GetValue(HighlightBorderBrushProperty);
        set => SetValue(HighlightBorderBrushProperty, value);
    }

    /// <inheritdoc cref="HighlightBorderThicknessProperty"/>
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

    /// <inheritdoc/>
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
