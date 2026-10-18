namespace Beresta.Markdown.Wpf.Presenters;

using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Extensions;
using Scrolling;
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

    /// <summary>Стиль клавиши <c>&lt;kbd&gt;</c> — того же <see cref="InlineCodePresenter"/>, что и код посреди строки.</summary>
    public static readonly DependencyProperty KbdStyleProperty =
        DependencyProperty.Register(
            nameof(KbdStyle),
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

    /// <summary>Стиль выделения маркером (<c>Span</c>).</summary>
    public static readonly DependencyProperty MarkedStyleProperty =
        DependencyProperty.Register(
            nameof(MarkedStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Стиль вставки (<c>Span</c>).</summary>
    public static readonly DependencyProperty InsertedStyleProperty =
        DependencyProperty.Register(
            nameof(InsertedStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Стиль названия произведения (<c>Span</c>).</summary>
    public static readonly DependencyProperty CiteStyleProperty =
        DependencyProperty.Register(
            nameof(CiteStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Стиль сокращения с расшифровкой в подсказке (<c>Span</c>).</summary>
    public static readonly DependencyProperty AbbreviationStyleProperty =
        DependencyProperty.Register(
            nameof(AbbreviationStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Кисть пунктира под сокращением, если стиль сокращения не задаёт свои линии.</summary>
    public static readonly DependencyProperty AbbreviationUnderlineBrushProperty =
        DependencyProperty.Register(
            nameof(AbbreviationUnderlineBrush),
            typeof(Brush),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(Brushes.Gray, OnAnyPropertyChanged));

    private static readonly DependencyProperty LayerProperty =
        MdSelection.LayerProperty.AddOwner(
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnLayerChanged));

    private static readonly DependencyProperty StatesProperty =
        MdWidget.StatesProperty.AddOwner(
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnStatesChanged));

    private static readonly DependencyProperty GapsProperty =
        GapLayout.LayoutProperty.AddOwner(
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnGapsChanged));

    private static readonly DataTemplateSelector SharedSelector = new MarkdownTemplateSelector();

    /// <summary>Размер шрифта индекса <c>&lt;sub&gt;</c> и <c>&lt;sup&gt;</c> от размера текста.</summary>
    private static readonly IValueConverter IndexFontSize = new MdScaleConverter(0.75);

    private readonly List<Segment> _segments = [];
    private readonly List<(RunVm Run, Span Span)> _styled = [];
    private readonly List<(Border Space, double Height)> _gapRows = [];
    private List<Highlight> _highlights = [];
    private IReadOnlyList<(int Offset, double Height)> _gaps = [];
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

    public Style? KbdStyle
    {
        get => (Style?)GetValue(KbdStyleProperty);
        set => SetValue(KbdStyleProperty, value);
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

    public Style? MarkedStyle
    {
        get => (Style?)GetValue(MarkedStyleProperty);
        set => SetValue(MarkedStyleProperty, value);
    }

    public Style? InsertedStyle
    {
        get => (Style?)GetValue(InsertedStyleProperty);
        set => SetValue(InsertedStyleProperty, value);
    }

    public Style? CiteStyle
    {
        get => (Style?)GetValue(CiteStyleProperty);
        set => SetValue(CiteStyleProperty, value);
    }

    public Style? AbbreviationStyle
    {
        get => (Style?)GetValue(AbbreviationStyleProperty);
        set => SetValue(AbbreviationStyleProperty, value);
    }

    public Brush? AbbreviationUnderlineBrush
    {
        get => (Brush?)GetValue(AbbreviationUnderlineBrushProperty);
        set => SetValue(AbbreviationUnderlineBrushProperty, value);
    }

    /// <summary>Подсвеченные куски своего текста в порядке рисования.</summary>
    internal IEnumerable<(int Start, int End, MdMarkKind Kind, Brush Brush)> Highlights
        => _highlights.Select(highlight => (highlight.Start, highlight.End, highlight.Kind, highlight.Brush));

    /// <summary>Строки исходника, которые начинаются в этом тексте, и экранные строки, где они начинаются.</summary>
    internal IReadOnlyList<(int Line, Rect Row)> SourceRows => _sourceRows ??= FindSourceRows();

    /// <summary>Пустые места между строками своего текста во всю его ширину, в своих координатах.</summary>
    internal IEnumerable<Rect> GapRects
        => _gapRows
            .Where(row => row.Space.IsDescendantOf(this))
            .Select(row => new Rect(0, row.Space.TranslatePoint(default, this).Y, ActualWidth, row.Height));

    /// <summary>Экранная строка знака своего текста; такого знака нет — <c>null</c>.</summary>
    internal Rect? RowAt(int offset)
    {
        foreach (var segment in _segments)
        {
            if (segment.Element is Run run && offset >= segment.Start && offset < segment.End
                && run.ContentStart.GetPositionAtOffset(offset - segment.Start) is { } pointer)
            {
                return pointer.GetCharacterRect(LogicalDirection.Forward);
            }
        }

        return null;
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

    /// <summary>
    /// Место черты перед знаком своего текста — без ширины, высотой с его экранную строку; за концом
    /// текста — после последнего знака.
    /// </summary>
    internal Rect? GetCaretRect(int offset)
    {
        if (_segments.Count == 0)
        {
            return null;
        }

        var index = _segments.FindIndex(segment => offset >= segment.Start && offset < segment.End);
        var after = index < 0;
        var segment = _segments[after ? ^1 : index];
        if (segment.Overlay is { } overlay)
        {
            if (!overlay.IsDescendantOf(this))
            {
                return null;
            }

            var bounds = overlay.TransformToAncestor(this).TransformBounds(new(overlay.RenderSize));
            return new(after ? bounds.Right : bounds.Left, bounds.Top, 0, bounds.Height);
        }

        var pointer = segment.Element switch
        {
            Run run when !after => run.ContentStart.GetPositionAtOffset(offset - segment.Start, LogicalDirection.Forward),
            Run run => run.ContentEnd,
            var element => after ? element.ElementEnd : element.ElementStart
        };
        var direction = !after && segment.Element is Run ? LogicalDirection.Forward : LogicalDirection.Backward;
        return pointer?.GetCharacterRect(direction) is { IsEmpty: false } rect ? new(rect.Left, rect.Top, 0, rect.Height) : null;
    }

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

    /// <summary>Над текстом, пока выделение включено, — текстовый курсор; ссылки и виджеты ставят свой.</summary>
    protected override void OnQueryCursor(QueryCursorEventArgs e)
    {
        base.OnQueryCursor(e);
        if (!e.Handled
            && e.OriginalSource is TextElement or MdTextBlock
            && GetValue(LayerProperty) is MdDecorationLayer { IsSelectionEnabled: true })
        {
            e.Cursor = Cursors.IBeam;
            e.Handled = true;
        }
    }

    /// <summary>
    /// Ширина самого длинного куска, который нельзя перенести: слова или неделимого элемента. Элементы
    /// берут ширину последнего измерения.
    /// </summary>
    internal double LongestWordWidth()
    {
        var text = new StringBuilder();
        var runs = new List<(Run Run, int Start)>();
        var widest = 0.0;
        foreach (var segment in _segments)
        {
            switch (segment.Element)
            {
                case Run run:
                    runs.Add((run, text.Length));
                    text.Append(run.Text);
                    break;

                case InlineUIContainer { Child: { } child }:
                    widest = Math.Max(widest, child.DesiredSize.Width);
                    text.Append(' ');
                    break;

                default:
                    text.Append(' ');
                    break;
            }
        }

        if (runs.Count == 0)
        {
            return widest;
        }

        var formatted = new FormattedText(
            text.ToString(),
            CultureInfo.CurrentUICulture,
            FlowDirection,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            Foreground ?? Brushes.Black,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        foreach (var (run, start) in runs.Where(entry => entry.Run.Text.Length > 0))
        {
            formatted.SetFontFamily(run.FontFamily, start, run.Text.Length);
            formatted.SetFontSize(run.FontSize, start, run.Text.Length);
            formatted.SetFontWeight(run.FontWeight, start, run.Text.Length);
            formatted.SetFontStyle(run.FontStyle, start, run.Text.Length);
        }

        return Math.Max(widest, formatted.MinWidth);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        _sourceRows = null;
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

    private static void OnLayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdTextBlock)d).UpdateHighlights();

    private static void OnStatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdTextBlock)d).UpdateWidgetStates();

    private static void OnGapsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var text = (MdTextBlock)d;
        if (!text.GapsOfRuns().SequenceEqual(text._gaps))
        {
            text.Rebuild();
        }
    }

    private static IEnumerable<Rect> GetRunRects(Segment segment, int start, int end)
    {
        if (segment.Element is not Run run)
        {
            yield break;
        }

        // BYDESIGN: начало куска смотрит вперёд, а конец назад: на месте переноса начало иначе считается
        // концом прежней экранной строки, и подсветка ложится на две строки сразу.
        var from = run.ContentStart.GetPositionAtOffset(Math.Max(start, segment.Start) - segment.Start, LogicalDirection.Forward);
        var to = run.ContentStart.GetPositionAtOffset(Math.Min(end, segment.End) - segment.Start, LogicalDirection.Backward);
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
    private static (InlineUIContainer Container, Border Overlay) CreateAtomic(
        UIElement content,
        BaselineAlignment alignment = BaselineAlignment.Center)
    {
        var overlay = new Border { IsHitTestVisible = false };
        var host = new Grid { Children = { content, overlay } };
        var container = new InlineUIContainer(host) { BaselineAlignment = alignment };
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
            foreach (var (part, kind, brush) in layer.Within(range))
            {
                highlights.Add(new(part.Start - range.Start, part.End - range.Start, kind, brush));
            }
        }

        foreach (var segment in _segments)
        {
            if (segment.Overlay is { } overlay)
            {
                var covering = highlights.Where(h => h.Start < segment.End && h.End > segment.Start).ToList();
                MarkLook.Apply(overlay, this, covering.Select(h => (h.Kind, h.Brush)));
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
        if (_highlights.Count == 0 && _gapRows.Count == 0)
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
        _styled.Clear();
        _gapRows.Clear();
        _sourceRows = null;
        _gaps = GapsOfRuns();
        var nextGap = 0;

        foreach (var inline in Runs ?? [])
        {
            var start = _segments.Count == 0 ? 0 : _segments[^1].End;
            var length = MdTextMap.TextOf(inline).Length;
            AddGapRows(start, ref nextGap);

            switch (inline)
            {
                case LineBreakVm:
                    var lineBreak = new LineBreak();
                    Inlines.Add(lineBreak);
                    _segments.Add(new(inline, start, length, lineBreak, null, null));
                    break;

                case RunVm { Code: true } or RunVm { Kbd: true }:
                    var boxed = (RunVm)inline;
                    var presenter = new InlineCodePresenter
                    {
                        Text = boxed.Text,
                        Style = boxed.Code ? InlineCodeStyle : KbdStyle,
                        TextDecorations = DecorationsOf(boxed)
                    };

                    if (boxed.Hyperlink is not null && LinkForeground is { } linkForeground)
                    {
                        presenter.Foreground = linkForeground;
                    }

                    var (container, overlay) = CreateAtomic(presenter);
                    Inlines.Add(Dress(boxed, container));
                    _segments.Add(new(inline, start, length, container, overlay, null));
                    break;

                case RunVm run:
                    AddRunPieces(run, start, length, ref nextGap);
                    break;

                default:
                    var widget = new ContentPresenter
                    {
                        Content = inline,
                        ContentTemplateSelector = TryFindResource("Md.TemplateSelector") as DataTemplateSelector ?? SharedSelector
                    };
                    var (host, cover) = CreateAtomic(
                        widget,
                        inline is FootnoteLinkVm { IsBack: false } ? BaselineAlignment.Top : BaselineAlignment.Center);
                    Inlines.Add(host);
                    _segments.Add(new(inline, start, length, host, cover, widget));
                    break;
            }
        }

        AddGapRows(int.MaxValue, ref nextGap);
        _highlights = [];
        SyncAdorner();
        UpdateHighlights();
        UpdateWidgetStates();
    }

    private IReadOnlyList<(int Offset, double Height)> GapsOfRuns()
        => GetValue(GapsProperty) is GapLayout layout && Runs is { } runs
            ? layout.InText(runs).OrderBy(gap => gap.Offset).ToList()
            : [];

    /// <summary>Пустые места, которые встают до позиции <paramref name="position"/> своего текста; за концом текста — под ним.</summary>
    private void AddGapRows(int position, ref int nextGap)
    {
        var afterText = position == int.MaxValue && _segments.Count > 0;
        for (; nextGap < _gaps.Count && _gaps[nextGap].Offset <= position; nextGap++)
        {
            // BYDESIGN: место — строка из одного вставного элемента: такая строка ровно его высоты,
            // и соседние экранные строки текста не меняются.
            var space = new Border { Width = 1, Height = _gaps[nextGap].Height, IsHitTestVisible = false };
            if (afterText)
            {
                Inlines.Add(new LineBreak());
                Inlines.Add(new InlineUIContainer(space));
            }
            else
            {
                Inlines.Add(new InlineUIContainer(space));
                Inlines.Add(new LineBreak());
            }

            _gapRows.Add((space, _gaps[nextGap].Height));
        }
    }

    /// <summary>Кусок текста — одним прогоном, а с пустыми местами внутри — прогоном на каждую часть между ними.</summary>
    private void AddRunPieces(RunVm run, int start, int length, ref int nextGap)
    {
        var text = run.Text ?? string.Empty;
        var from = 0;
        while (nextGap < _gaps.Count && _gaps[nextGap].Offset < start + length)
        {
            var cut = _gaps[nextGap].Offset - start;
            _segments.Add(new(run, start + from, cut - from, AddRun(run, text[from..cut]), null, null));
            from = cut;
            AddGapRows(start + cut, ref nextGap);
        }

        _segments.Add(new(run, start + from, length - from, AddRun(run, text[from..]), null, null));
    }

    /// <summary>Для каждой строки исходника — экранная строка её первого инлайна.</summary>
    private List<(int Line, Rect Row)> FindSourceRows()
    {
        var rows = new List<(int Line, Rect Row)>();
        foreach (var segment in _segments)
        {
            var source = segment.Inline.Source;
            if (segment.Inline is LineBreakVm || source.IsEmpty || (rows is [.., var last] && source.StartLine <= last.Line))
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

        foreach (var (run, span) in _styled)
        {
            MdWidget.SetState(span, states?.StateOf(run));
        }
    }

    /// <summary>Щелчок по оформленному куску без выделения текста отдаётся его состоянию.</summary>
    private void OnStyledClick(object sender, MouseButtonEventArgs e)
    {
        if (MdWidget.GetState((Span)sender) is IMdClickableState state && MarkdownViewer.Owning(this) is not { SelectionLength: > 0 })
        {
            state.OnClick();
            e.Handled = true;
        }
    }

    private Run AddRun(RunVm run, string content)
    {
        var text = new Run(content);
        Inlines.Add(Dress(run, text));
        return text;
    }

    /// <summary>Кусок текста или код в начертаниях, оформлении и ссылке своего инлайна.</summary>
    private Inline Dress(RunVm run, Inline inner)
    {
        var current = inner;
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

        if (run.Cite)
        {
            current = Wrap(current, CiteStyle, span => span.FontStyle = FontStyles.Italic);
        }

        if (run.Inserted)
        {
            current = Wrap(current, InsertedStyle, span => span.TextDecorations = System.Windows.TextDecorations.Underline);
        }

        if (run.Marked)
        {
            current = Wrap(current, MarkedStyle, span => span.Background = Brushes.Yellow);
        }

        if (run.Abbreviation is { } expansion)
        {
            var abbreviation = Wrap(current, AbbreviationStyle, _ => { });
            abbreviation.ToolTip = expansion;
            if (DependencyPropertyHelper.GetValueSource(abbreviation, Inline.TextDecorationsProperty).BaseValueSource == BaseValueSource.Default)
            {
                abbreviation.TextDecorations = AbbreviationUnderline();
            }

            current = abbreviation;
        }

        if (run.Subscript || run.Superscript)
        {
            var shifted = new Span(current)
            {
                BaselineAlignment = run.Subscript ? BaselineAlignment.Subscript : BaselineAlignment.TextTop
            };
            shifted.SetBinding(TextElement.FontSizeProperty, new Binding(nameof(FontSize)) { Source = this, Converter = IndexFontSize });
            current = shifted;
        }

        if (run.StyleName is { } name)
        {
            var styled = new Span(current);
            styled.SetResourceReference(FrameworkContentElement.StyleProperty, $"Md.{name}.Style");
            styled.MouseLeftButtonUp += OnStyledClick;
            _styled.Add((run, styled));
            current = styled;
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
            link.ToolTip = run.LinkTitle ?? href;
            link.RequestNavigate += (_, e) =>
            {
                e.Handled = true;
                MarkdownLink.Activate(this, href, e.Uri);
            };

            link.Inlines.Add(current);
            current = link;
        }

        return current;
    }

    /// <summary>Пунктир под сокращением кистью <see cref="AbbreviationUnderlineBrush"/>.</summary>
    private TextDecorationCollection AbbreviationUnderline()
    {
        var pen = new Pen(AbbreviationUnderlineBrush ?? Brushes.Gray, 1) { DashStyle = DashStyles.Dot };
        var underline = new TextDecorationCollection
        {
            new TextDecoration(TextDecorationLocation.Underline, pen, 1, TextDecorationUnit.FontRecommended, TextDecorationUnit.FontRecommended)
        };
        underline.Freeze();
        return underline;
    }

    /// <summary>Линии по коду от начертаний вокруг: зачёркивание, подчёркивание вставки.</summary>
    private static TextDecorationCollection? DecorationsOf(RunVm run)
        => (run.Strike, run.Inserted) switch
        {
            (true, true) => [.. System.Windows.TextDecorations.Strikethrough, .. System.Windows.TextDecorations.Underline],
            (true, false) => System.Windows.TextDecorations.Strikethrough,
            (false, true) => System.Windows.TextDecorations.Underline,
            _ => null
        };

    /// <summary>Кусок своего текста: где он в карте и каким элементом нарисован.</summary>
    private readonly record struct Segment(InlineVm Inline, int Start, int Length, TextElement Element, Border? Overlay, ContentPresenter? Widget)
    {
        public int End => Start + Length;
    }

    private sealed record Highlight(int Start, int End, MdMarkKind Kind, Brush Brush);
}