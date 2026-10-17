namespace MarkdigWpfViewer.Presenters;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Selection;
using ViewModels;

/// <summary>
/// Поля с номерами строк исходника — у каждой строки, у которой есть своё место на экране.
/// Ширина общая на документ — по числу цифр последней строки. Значки пометок стоят слева от
/// номеров, полосы — справа.
/// </summary>
public sealed class MdLineGutter : Control
{
    /// <summary>Ширина полосы пометки вида <see cref="MdMarkKind.Bar"/>.</summary>
    public static readonly DependencyProperty BarWidthProperty =
        DependencyProperty.Register(
            nameof(BarWidth),
            typeof(double),
            typeof(MdLineGutter),
            new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Сторона значка пометки вида <see cref="MdMarkKind.Icon"/>.</summary>
    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(
            nameof(IconSize),
            typeof(double),
            typeof(MdLineGutter),
            new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    private const int MaxLabels = 4096;

    private static readonly DependencyProperty LayerProperty =
        MdSelection.LayerProperty.AddOwner(
            typeof(MdLineGutter),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnLayerChanged));

    private readonly Dictionary<int, FormattedText> _labels = [];

    static MdLineGutter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MdLineGutter), new FrameworkPropertyMetadata(typeof(MdLineGutter)));
    }

    public double BarWidth
    {
        get => (double)GetValue(BarWidthProperty);
        set => SetValue(BarWidthProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <summary>Сколько раз поля рисовались.</summary>
    internal int Renders { get; private set; }

    private double BarColumn => GetValue(LayerProperty) is MdDecorationLayer layer && layer.Has(MdMarkKind.Bar) ? BarWidth : 0;

    private double IconColumn => GetValue(LayerProperty) is MdDecorationLayer layer && layer.Has(MdMarkKind.Icon) ? IconSize : 0;

    /// <summary>Полосы и значки пометок у созданных блоков в координатах полей, в порядке рисования.</summary>
    internal IReadOnlyList<(MdMarkKind Kind, Rect Area, Brush Brush, Geometry? Icon)> CollectMarks()
    {
        var marks = new List<(MdMarkKind Kind, Rect Area, Brush Brush, Geometry? Icon)>();
        if (TemplatedParent is not MarkdownViewer viewer
            || GetValue(LayerProperty) is not MdDecorationLayer layer
            || viewer.RealizedLines() is not { } realized)
        {
            return marks;
        }

        foreach (var mark in layer.LinesWithin(realized.First, realized.Last, MdMarkKind.Bar))
        {
            if (viewer.GetLineBand(Math.Max(mark.First, realized.First), Math.Min(mark.Last, realized.Last), this) is { } band)
            {
                marks.Add((MdMarkKind.Bar, new(ActualWidth - BarColumn, band.Top, BarColumn, band.Height), mark.Brush!, null));
            }
        }

        foreach (var mark in layer.LinesWithin(realized.First, realized.Last, MdMarkKind.Icon))
        {
            if (mark.Mark.Icon is { } icon && mark.First >= realized.First && viewer.GetLineBand(mark.First, mark.First, this) is { } band)
            {
                var row = Math.Min(band.Height, Label(mark.First).Height);
                var size = Math.Min(IconColumn, row);
                marks.Add((MdMarkKind.Icon, new(0, band.Top + (row - size) / 2, size, size), mark.Brush!, icon));
            }
        }

        return marks;
    }

    /// <summary>Номера созданных блоков: строка исходника, верх и высота её экранной строки в координатах полей.</summary>
    internal IReadOnlyList<(int Line, double Top, double Height)> CollectNumbers()
    {
        if (TemplatedParent is not MarkdownViewer { BlockPanel: { } panel })
        {
            return [];
        }

        var numbers = new SortedDictionary<int, Place>();
        Collect(panel, null, numbers);
        return WithoutOverlaps(numbers);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var digits = Math.Max(2, (LastLine() + 1).ToString(CultureInfo.InvariantCulture).Length);
        var width = Format(new('9', digits)).WidthIncludingTrailingWhitespace;
        return new(IconColumn + Math.Ceiling(width) + Padding.Left + Padding.Right + BarColumn, 0);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        Renders++;

        foreach (var (kind, area, brush, icon) in CollectMarks())
        {
            if (kind == MdMarkKind.Icon && icon is { Bounds: { IsEmpty: false } bounds })
            {
                var scale = Math.Min(area.Width / Math.Max(bounds.Width, 1e-6), area.Height / Math.Max(bounds.Height, 1e-6));
                var transform = new Matrix();
                transform.Translate(-bounds.Left, -bounds.Top);
                transform.Scale(scale, scale);
                transform.Translate(area.Left + (area.Width - bounds.Width * scale) / 2, area.Top + (area.Height - bounds.Height * scale) / 2);
                drawingContext.PushTransform(new MatrixTransform(transform));
                drawingContext.DrawGeometry(brush, null, icon);
                drawingContext.Pop();
            }
            else
            {
                drawingContext.DrawRectangle(brush, null, area);
            }
        }

        var right = ActualWidth - Padding.Right - BarColumn;
        foreach (var (line, top, height) in CollectNumbers())
        {
            var label = Label(line);
            drawingContext.DrawText(label, new(right - label.WidthIncludingTrailingWhitespace, top + (height - label.Height) / 2));
        }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == FontFamilyProperty
            || e.Property == FontSizeProperty
            || e.Property == FontStyleProperty
            || e.Property == FontWeightProperty
            || e.Property == FontStretchProperty
            || e.Property == PaddingProperty)
        {
            _labels.Clear();
            InvalidateMeasure();
            InvalidateVisual();
        }
        else if (e.Property == ForegroundProperty)
        {
            _labels.Clear();
            InvalidateVisual();
        }
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        _labels.Clear();
    }

    /// <summary>Сменились пометки — поля перерисовываются; одно выделение их не трогает.</summary>
    private static void OnLayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (ReferenceEquals((e.OldValue as MdDecorationLayer)?.Marks, (e.NewValue as MdDecorationLayer)?.Marks))
        {
            return;
        }

        var gutter = (MdLineGutter)d;
        gutter.InvalidateMeasure();
        gutter.InvalidateVisual();
    }

    /// <summary>Из номеров, налезающих друг на друга, оставляет один: на своей строке — охотнее, чем у рамки.</summary>
    private static List<(int Line, double Top, double Height)> WithoutOverlaps(SortedDictionary<int, Place> numbers)
    {
        var placed = new List<(int Line, Place Place)>();

        foreach (var (line, place) in numbers.OrderBy(pair => pair.Value.Top))
        {
            if (placed.Count > 0 && placed[^1].Place is var last && last.Top + last.Height > place.Top + 0.5)
            {
                if (place.Exact && !last.Exact)
                {
                    placed[^1] = (line, place);
                }

                continue;
            }

            placed.Add((line, place));
        }

        return placed.OrderBy(item => item.Line).Select(item => (item.Line, item.Place.Top, item.Place.Height)).ToList();
    }

    /// <summary>Свою строку предпочитает рамке блока, из равных — верхнюю.</summary>
    private static void Remember(SortedDictionary<int, Place> numbers, int line, Place place)
    {
        if (!numbers.TryGetValue(line, out var known)
            || (place.Exact != known.Exact ? place.Exact : place.Top < known.Top))
        {
            numbers[line] = place;
        }
    }

    /// <summary>
    /// Обходит созданные блоки. Внутрь текста и кода не заходит: свои строки они знают сами; блок
    /// без своих строк получает номер первой строки у верха рамки.
    /// </summary>
    private void Collect(DependencyObject node, NodeVm? owner, SortedDictionary<int, Place> numbers)
    {
        switch (node)
        {
            case MdTextBlock { ActualHeight: > 0 } text:
                RememberRows(numbers, text, text.SourceRows);
                return;

            case CodeBlockPresenter { ActualHeight: > 0 } code:
                RememberRows(numbers, code, code.SourceRows());
                return;
        }

        var content = node switch
        {
            ContentPresenter presenter => presenter.Content,
            ContentControl control => control.Content,
            _ => null
        };

        if (node is FrameworkElement { ActualHeight: > 0 } element
            && content is NodeVm { Source.IsEmpty: false } model and not InlineVm
            && !ReferenceEquals(model, owner))
        {
            var top = element.TranslatePoint(default, this).Y;
            if (model is BlankLineVm)
            {
                Remember(numbers, model.Source.StartLine, new(top, element.ActualHeight, Exact: true));
                return;
            }

            var height = Math.Min(Label(model.Source.StartLine).Height, element.ActualHeight);
            Remember(numbers, model.Source.StartLine, new(top, height, Exact: false));
            owner = model;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            Collect(VisualTreeHelper.GetChild(node, i), owner, numbers);
        }
    }

    private void RememberRows(SortedDictionary<int, Place> numbers, UIElement element, IEnumerable<(int Line, Rect Row)> rows)
    {
        var top = element.TranslatePoint(default, this).Y;
        foreach (var (line, row) in rows)
        {
            Remember(numbers, line, new(top + row.Top, row.Height, Exact: true));
        }
    }

    private int LastLine()
    {
        return TemplatedParent is MarkdownViewer { ItemsSource: IReadOnlyList<BlockVm> { Count: > 0 } items }
            ? items.Max(item => item.Source.EndLine)
            : 0;
    }

    private FormattedText Label(int line)
    {
        if (_labels.TryGetValue(line, out var label))
        {
            return label;
        }

        if (_labels.Count >= MaxLabels)
        {
            _labels.Clear();
        }

        return _labels[line] = Format((line + 1).ToString(CultureInfo.InvariantCulture));
    }

    private FormattedText Format(string text)
    {
        return new(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            Foreground ?? Brushes.Gray,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    /// <summary>Где стоит номер: полоса по вертикали и своя ли это строка или только рамка блока.</summary>
    private readonly record struct Place(double Top, double Height, bool Exact);
}