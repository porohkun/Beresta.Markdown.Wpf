namespace MarkdigWpfViewer.Presenters;

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Extensions;
using Selection;
using Styling;
using ViewModels;

/// <summary>Текст блока из инлайнов: начертания, ссылки, код посреди строки, строчные виджеты.</summary>
public class MdTextBlock : TextBlock
{
    public static readonly DependencyProperty RunsProperty =
        DependencyProperty.Register(
            nameof(Runs),
            typeof(IEnumerable<InlineVm>),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    public static readonly DependencyProperty LinkForegroundProperty =
        DependencyProperty.Register(
            nameof(LinkForeground),
            typeof(Brush),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(Brushes.Blue, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    public static readonly DependencyProperty InlineCodeStyleProperty =
        DependencyProperty.Register(
            nameof(InlineCodeStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Стиль жирного (<c>Span</c>).</summary>
    public static readonly DependencyProperty StrongStyleProperty =
        DependencyProperty.Register(
            nameof(StrongStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Стиль курсива (<c>Span</c>).</summary>
    public static readonly DependencyProperty EmphasisStyleProperty =
        DependencyProperty.Register(
            nameof(EmphasisStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Стиль зачёркнутого (<c>Span</c>).</summary>
    public static readonly DependencyProperty StrikeStyleProperty =
        DependencyProperty.Register(
            nameof(StrikeStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Стиль ссылки (<c>Hyperlink</c>); цвет задаёт <see cref="LinkForeground"/>.</summary>
    public static readonly DependencyProperty LinkStyleProperty =
        DependencyProperty.Register(
            nameof(LinkStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    private static readonly DependencyProperty LayerProperty =
        MdSelection.LayerProperty.AddOwner(
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnLayerChanged));

    private static readonly DependencyProperty StatesProperty =
        MdWidget.StatesProperty.AddOwner(
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnStatesChanged));

    private static readonly string[] ExternalSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto];

    private static readonly DataTemplateSelector SharedSelector = new MarkdownTemplateSelector();

    private readonly List<Segment> _segments = [];
    private List<Highlight> _highlights = [];
    private List<(int Line, Rect Row)>? _sourceRows;
    private AdornerLayer? _adornerLayer;
    private MdHighlightAdorner? _adorner;

    public MdTextBlock()
    {
        Rebuild();
        Loaded += (_, _) => SyncAdorner();
        Unloaded += (_, _) =>
        {
            _adornerLayer?.Remove(_adorner);
            _adornerLayer = null;
            _adorner = null;
        };
    }

    public IEnumerable<InlineVm>? Runs
    {
        get => (IEnumerable<InlineVm>?)GetValue(RunsProperty);
        set => SetValue(RunsProperty, value);
    }

    public Brush LinkForeground
    {
        get => (Brush)GetValue(LinkForegroundProperty);
        set => SetValue(LinkForegroundProperty, value);
    }

    public Style InlineCodeStyle
    {
        get => (Style)GetValue(InlineCodeStyleProperty);
        set => SetValue(InlineCodeStyleProperty, value);
    }

    public Style? StrongStyle
    {
        get => (Style?)GetValue(StrongStyleProperty);
        set => SetValue(StrongStyleProperty, value);
    }

    public Style? EmphasisStyle
    {
        get => (Style?)GetValue(EmphasisStyleProperty);
        set => SetValue(EmphasisStyleProperty, value);
    }

    public Style? StrikeStyle
    {
        get => (Style?)GetValue(StrikeStyleProperty);
        set => SetValue(StrikeStyleProperty, value);
    }

    public Style? LinkStyle
    {
        get => (Style?)GetValue(LinkStyleProperty);
        set => SetValue(LinkStyleProperty, value);
    }

    /// <summary>Подсвеченные куски своего текста в порядке рисования.</summary>
    internal IEnumerable<(int Start, int End, Brush Brush)> Highlights
        => _highlights.Select(highlight => (highlight.Start, highlight.End, highlight.Brush));

    /// <summary>Строки исходника, которые начинаются в этом тексте, и экранные строки, где они начинаются.</summary>
    internal IReadOnlyList<(int Line, Rect Row)> SourceRows => _sourceRows ??= FindSourceRows();

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        _sourceRows = null;
    }

    /// <summary>Кусок текстовой карты, который рисует этот блок.</summary>
    internal bool TryGetMapRange(out MdTextRange range)
    {
        range = default;
        return GetValue(LayerProperty) is MdDecorationLayer layer
               && Runs is { } runs
               && layer.Map.TryGetRange(runs, out range);
    }

    /// <summary>Смещение в своём куске карты для точки в своих координатах.</summary>
    internal int HitTestLocal(Point point)
    {
        var length = _segments.Count == 0 ? 0 : _segments[^1].End;
        if (point.Y < 0)
        {
            return 0;
        }

        if (point.Y >= ActualHeight)
        {
            return length;
        }

        var clamped = new Point(Math.Clamp(point.X, 0, ActualWidth), point.Y);
        var pointer = GetPositionFromPoint(clamped, snapToText: true);
        return pointer is null ? point.X <= 0 ? 0 : length : OffsetOf(pointer, clamped);
    }

    /// <summary>Прямоугольники подсветки своего куска в своих координатах.</summary>
    internal IEnumerable<Rect> GetRangeRects(int start, int end)
        => _segments
            .Where(segment => segment.End > start && segment.Start < end)
            .SelectMany(segment => GetRunRects(segment, start, end));

    /// <summary>Прямоугольники своего куска в своих координатах: строки текста и неделимые куски целиком.</summary>
    internal IEnumerable<Rect> GetBounds(int start, int end)
    {
        foreach (var segment in _segments.Where(segment => segment.End > start && segment.Start < end))
        {
            if (segment.Overlay is { } overlay)
            {
                if (overlay.IsDescendantOf(this))
                {
                    yield return overlay.TransformToAncestor(this).TransformBounds(new(overlay.RenderSize));
                }

                continue;
            }

            foreach (var rect in GetRunRects(segment, start, end))
            {
                yield return rect;
            }
        }
    }

    private static void OnAnyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdTextBlock)d).Rebuild();

    private static Span Wrap(Inline inner, Style? style, Action<Span> fallback)
    {
        var span = new Span(inner);
        if (style is null)
        {
            fallback(span);
        }
        else
        {
            span.Style = style;
        }

        return span;
    }

    /// <summary>
    /// Ссылка наружу — системным обработчиком. Открываются только адреса веба и почты;
    /// <c>www.example.com</c> без схемы считается адресом веба.
    /// </summary>
    private static void OpenExternal(string href)
    {
        var target = href.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? "http://" + href : href;
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri) || !ExternalSchemes.Contains(uri.Scheme))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch
        {
            // deliberately ignored
        }
    }

    private static void OnLayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdTextBlock)d).UpdateHighlights();

    private static void OnStatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdTextBlock)d).UpdateWidgetStates();

    private static IEnumerable<Rect> GetRunRects(Segment segment, int start, int end)
    {
        if (segment.Element is not Run run)
        {
            yield break;
        }

        var from = run.ContentStart.GetPositionAtOffset(Math.Max(start, segment.Start) - segment.Start);
        var to = run.ContentStart.GetPositionAtOffset(Math.Min(end, segment.End) - segment.Start);
        if (from is null || to is null)
        {
            yield break;
        }

        foreach (var rect in GetLineRects(from, to))
        {
            yield return rect;
        }
    }

    private static IEnumerable<Rect> GetLineRects(TextPointer from, TextPointer to)
    {
        var lineStart = from;
        while (lineStart.CompareTo(to) < 0)
        {
            var next = lineStart.GetLineStartPosition(1, out var moved);
            var lineEnd = next is null || moved == 0 || next.CompareTo(to) >= 0 ? to : next;
            var first = lineStart.GetCharacterRect(LogicalDirection.Forward);
            var last = lineEnd.GetCharacterRect(LogicalDirection.Backward);
            if (last.Left > first.Left)
            {
                yield return new(first.Left, first.Top, last.Left - first.Left, first.Height);
            }

            if (ReferenceEquals(lineEnd, to))
            {
                yield break;
            }

            lineStart = lineEnd;
        }
    }

    /// <summary>Неделимый кусок строки с накладкой подсветки поверх.</summary>
    private static (InlineUIContainer Container, Border Overlay) CreateAtomic(UIElement content)
    {
        var overlay = new Border { IsHitTestVisible = false };
        var host = new Grid { Children = { content, overlay } };
        var container = new InlineUIContainer(host) { BaselineAlignment = BaselineAlignment.Center };
        return (container, overlay);
    }

    private int OffsetOf(TextPointer pointer, Point point)
    {
        foreach (var segment in _segments)
        {
            var element = segment.Element;
            if (pointer.CompareTo(element.ElementEnd) > 0)
            {
                continue;
            }

            if (element is Run run && pointer.CompareTo(run.ContentStart) >= 0)
            {
                return segment.Start + Math.Clamp(run.ContentStart.GetOffsetToPosition(pointer), 0, segment.Length);
            }

            if (segment.Overlay is { } overlay)
            {
                var bounds = overlay.TransformToAncestor(this).TransformBounds(new(overlay.RenderSize));
                return point.X > bounds.Left + bounds.Width / 2 ? segment.End : segment.Start;
            }

            return pointer.CompareTo(element.ElementEnd) == 0 ? segment.End : segment.Start;
        }

        return _segments.Count == 0 ? 0 : _segments[^1].End;
    }

    private void UpdateHighlights()
    {
        var highlights = new List<Highlight>();
        if (TryGetMapRange(out var range) && GetValue(LayerProperty) is MdDecorationLayer layer)
        {
            foreach (var (part, brush) in layer.Within(range))
            {
                highlights.Add(new(part.Start - range.Start, part.End - range.Start, brush));
            }
        }

        foreach (var segment in _segments)
        {
            if (segment.Overlay is { } overlay)
            {
                overlay.Background = highlights.LastOrDefault(h => h.Start < segment.End && h.End > segment.Start)?.Brush;
            }
        }

        if (!highlights.SequenceEqual(_highlights))
        {
            _highlights = highlights;
            SyncAdorner();
        }
    }

    private void SyncAdorner()
    {
        if (_highlights.Count == 0)
        {
            _adornerLayer?.Remove(_adorner);
            _adornerLayer = null;
            _adorner = null;
            return;
        }

        if (_adorner is null)
        {
            _adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (_adornerLayer is null)
            {
                return;
            }

            _adorner = new(this);
            _adornerLayer.Add(_adorner);
        }

        _adorner.InvalidateVisual();
    }

    private void Rebuild()
    {
        Inlines.Clear();
        _segments.Clear();
        _sourceRows = null;

        foreach (var inline in Runs ?? [])
        {
            var start = _segments.Count == 0 ? 0 : _segments[^1].End;
            var length = MdTextMap.TextOf(inline).Length;

            switch (inline)
            {
                case LineBreakVm:
                    var lineBreak = new LineBreak();
                    Inlines.Add(lineBreak);
                    _segments.Add(new(inline, start, length, lineBreak, null, null));
                    break;

                case RunVm { Code: true } code:
                    var (container, overlay) = CreateAtomic(new InlineCodePresenter { Text = code.Text, Style = InlineCodeStyle });
                    Inlines.Add(container);
                    _segments.Add(new(inline, start, length, container, overlay, null));
                    break;

                case RunVm run:
                    _segments.Add(new(inline, start, length, AddRun(run), null, null));
                    break;

                default:
                    var widget = new ContentPresenter
                    {
                        Content = inline,
                        ContentTemplateSelector = TryFindResource("Md.TemplateSelector") as DataTemplateSelector ?? SharedSelector
                    };
                    var (host, cover) = CreateAtomic(widget);
                    Inlines.Add(host);
                    _segments.Add(new(inline, start, length, host, cover, widget));
                    break;
            }
        }

        _highlights = [];
        SyncAdorner();
        UpdateHighlights();
        UpdateWidgetStates();
    }

    /// <summary>Для каждой строки исходника — экранная строка её первого инлайна.</summary>
    private List<(int Line, Rect Row)> FindSourceRows()
    {
        var rows = new List<(int Line, Rect Row)>();
        foreach (var segment in _segments)
        {
            var source = segment.Inline.Source;
            if (segment.Inline is LineBreakVm || source.IsEmpty || rows is [.., var last] && source.StartLine <= last.Line)
            {
                continue;
            }

            var start = segment.Element is Run run ? run.ContentStart : segment.Element.ElementStart;
            if (start.GetCharacterRect(LogicalDirection.Forward) is { IsEmpty: false, Height: > 0 } row)
            {
                rows.Add((source.StartLine, row));
            }
        }

        return rows;
    }

    private void UpdateWidgetStates()
    {
        var states = GetValue(StatesProperty) as WidgetStates;
        foreach (var segment in _segments)
        {
            if (segment.Widget is { } widget)
            {
                MdWidget.SetState(widget, states?.StateOf(widget.Content));
            }
        }
    }

    private Run AddRun(RunVm run)
    {
        var text = new Run(run.Text ?? string.Empty);
        Inline current = text;

        if (run.Strike)
        {
            current = Wrap(current, StrikeStyle, span => span.TextDecorations = System.Windows.TextDecorations.Strikethrough);
        }

        if (run.Italic)
        {
            current = Wrap(current, EmphasisStyle, span => span.FontStyle = FontStyles.Italic);
        }

        if (run.Bold)
        {
            current = Wrap(current, StrongStyle, span => span.FontWeight = FontWeights.SemiBold);
        }

        if (run.Hyperlink is not null)
        {
            var link = new Hyperlink
            {
                NavigateUri = run.Hyperlink,
                Style = LinkStyle,
                Foreground = LinkForeground
            };

            // Подписка на каждую ссылку отдельно, а не одна на весь блок: исходный адрес
            // (inline.Href) в RequestNavigateEventArgs не поедет, а именно он нужен обработчику
            // своих ссылок — Uri их нормализует.
            var href = run.Href ?? run.Hyperlink.ToString();
            link.RequestNavigate += (_, e) =>
            {
                e.Handled = true;
                Navigate(href, e.Uri);
            };

            link.Inlines.Add(current);
            current = link;
        }

        Inlines.Add(current);
        return text;
    }

    /// <summary>
    /// Сначала спрашиваем приложение (<see cref="MarkdownLink.NavigateEvent"/> всплывает до
    /// любого предка вьюера), и только необработанную ссылку открываем наружу.
    /// </summary>
    private void Navigate(string href, Uri? uri)
    {
        var args = new MarkdownLinkEventArgs(MarkdownLink.NavigateEvent, this, href, uri);
        RaiseEvent(args);

        if (!args.Handled)
            OpenExternal(href);
    }

    /// <summary>Кусок своего текста: где он в карте и каким элементом нарисован.</summary>
    private readonly record struct Segment(InlineVm Inline, int Start, int Length, TextElement Element, Border? Overlay, ContentPresenter? Widget)
    {
        public int End => Start + Length;
    }

    private sealed record Highlight(int Start, int End, Brush Brush);
}