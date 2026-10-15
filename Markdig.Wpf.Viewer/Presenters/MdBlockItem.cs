namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Selection;

/// <summary>
/// Контейнер блока во вьюере и во вложенных блоках. Неделимый блок — разделитель, виджет без
/// текста — подсвечивается им целиком.
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
    }

    private static void OnLayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdBlockItem)d).UpdateHighlight();

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
