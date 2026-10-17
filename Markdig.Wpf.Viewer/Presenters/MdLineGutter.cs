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
    /// <param name="numbers">Уже собранные номера строк; нет — соберутся, если есть значки.</param>
    internal IReadOnlyList<(MdMarkKind Kind, Rect Area, Brush Brush, Geometry? Icon)> CollectMarks(
        IReadOnlyList<(int Line, double Top, double Height)>? numbers = null)
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

        if (!layer.Has(MdMarkKind.Icon))
        {
            return marks;
        }

        var places = (numbers ?? CollectNumbers()).ToDictionary(row => row.Line, row => (row.Top, row.Height));
        foreach (var mark in layer.LinesWithin(realized.First, realized.Last, MdMarkKind.Icon))
        {
            if (mark.Mark.Icon is { } icon && mark.First >= realized.First && IconRow(viewer, mark.First, places) is { } row)
            {
                var size = Math.Min(IconColumn, row.Height);
                marks.Add((MdMarkKind.Icon, new(0, row.Top + (row.Height - size) / 2, size, size), mark.Brush!, icon));
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

        return ScreenRows.Collect(panel, this, line => Label(line).Height);
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

        var numbers = CollectNumbers();
        foreach (var (kind, area, brush, icon) in CollectMarks(numbers))
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
        foreach (var (line, top, height) in numbers)
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

    /// <summary>Место значка строки: у её номера, а строке без номера — верх её полосы высотой с номер.</summary>
    private (double Top, double Height)? IconRow(MarkdownViewer viewer, int line, Dictionary<int, (double Top, double Height)> places)
    {
        if (places.TryGetValue(line, out var number))
        {
            return number;
        }

        return viewer.GetLineBand(line, line, this) is { } band
            ? (band.Top, Math.Min(band.Height, Label(line).Height))
            : null;
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
}