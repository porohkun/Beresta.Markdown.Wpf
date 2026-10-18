namespace MarkdigWpfViewer;

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Abstractions;
using Automation;
using Extensions;
using Identity;
using Markdig;
using Presenters;
using Scrolling;
using Search;
using Selection;
using ViewModels;

/// <summary>Контрол, который показывает markdown-текст блоками по шаблонам темы.</summary>
[TemplatePart(Name = GutterPart, Type = typeof(MdLineGutter))]
[TemplatePart(Name = RowMarksPart, Type = typeof(MdRowMarks))]
public sealed class MarkdownViewer : ItemsControl, IMdScrollSide
{
    /// <summary>Текст документа.</summary>
    public static readonly DependencyProperty MarkdownProperty =
        DependencyProperty.Register(
            nameof(Markdown),
            typeof(string),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.None, OnMarkdownChanged));

    /// <summary>
    /// Готовая настройка разбора Markdig; не задана — расширенная по умолчанию. С расширениями вьюера
    /// от неё берутся только расширения Markdig, настройки построителя — через <see cref="PipelineSetup"/>.
    /// </summary>
    public static readonly DependencyProperty MarkdownPipelineProperty =
        DependencyProperty.Register(
            nameof(MarkdownPipeline),
            typeof(MarkdownPipeline),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(null, OnPipelineChanged));

    /// <summary>Настройка разбора Markdig на новом построителе, поверх неё — расширения вьюера; задана — вместо <see cref="MarkdownPipeline"/>.</summary>
    public static readonly DependencyProperty PipelineSetupProperty =
        DependencyProperty.Register(
            nameof(PipelineSetup),
            typeof(Action<MarkdownPipelineBuilder>),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(null, OnPipelineChanged));

    /// <summary>
    /// Ключ документа: сменился — текст считается другим документом, и выделение, свёрнутость и другие
    /// состояния виджетов в него не переходят.
    /// </summary>
    public static readonly DependencyProperty DocumentKeyProperty =
        DependencyProperty.Register(
            nameof(DocumentKey),
            typeof(object),
            typeof(MarkdownViewer),
            new(null, OnDocumentKeyChanged));

    /// <summary>Шаблоны встроенных блоков.</summary>
    public static readonly DependencyProperty StyleProviderProperty =
        DependencyProperty.Register(
            nameof(StyleProvider),
            typeof(IMdStyleProvider),
            typeof(MarkdownViewer),
            new(null, OnContainersSourceChanged));

    /// <summary>Общие расширения нескольких вьюеров; заданные на окне, панели или в стиле, наследуются вьюерами внутри.</summary>
    public static readonly DependencyProperty SharedExtensionsProperty =
        DependencyProperty.RegisterAttached(
            nameof(SharedExtensions),
            typeof(MdExtensionCollection),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSharedExtensionsChanged));

    /// <summary>Подсветки кусков текста под слоями пометок и выделением: найденное, правки диффа.</summary>
    public static readonly DependencyProperty DecorationsProperty =
        DependencyProperty.Register(
            nameof(Decorations),
            typeof(IReadOnlyList<MdDecoration>),
            typeof(MarkdownViewer),
            new(null, OnDecorationsChanged));

    /// <summary>Пустые места перед строками исходника — например, чтобы две панели диффа стояли вровень.</summary>
    public static readonly DependencyProperty GapsProperty =
        DependencyProperty.Register(
            nameof(Gaps),
            typeof(IReadOnlyList<MdGap>),
            typeof(MarkdownViewer),
            new(null, OnGapsChanged));

    /// <summary>Кисть выделения; рисуется поверх текста и пометок.</summary>
    public static readonly DependencyProperty SelectionBrushProperty =
        DependencyProperty.Register(
            nameof(SelectionBrush),
            typeof(Brush),
            typeof(MarkdownViewer),
            new(null, OnSelectionBrushChanged));

    /// <summary>Можно ли выделять текст; выключение снимает выделение.</summary>
    public static readonly DependencyProperty IsSelectionEnabledProperty =
        DependencyProperty.Register(
            nameof(IsSelectionEnabled),
            typeof(bool),
            typeof(MarkdownViewer),
            new(true, OnIsSelectionEnabledChanged));

    /// <summary>Берёт ли вьюер Ctrl+C сам; выключенный отдаёт команду копирования приложению.</summary>
    public static readonly DependencyProperty IsCopyEnabledProperty =
        DependencyProperty.Register(
            nameof(IsCopyEnabled),
            typeof(bool),
            typeof(MarkdownViewer),
            new(true, (_, _) => CommandManager.InvalidateRequerySuggested()));

    /// <summary>Откуда берутся картинки; по умолчанию <see cref="MdImageLoader"/>.</summary>
    public static readonly DependencyProperty ImageLoaderProperty =
        DependencyProperty.Register(
            nameof(ImageLoader),
            typeof(IMdImageLoader),
            typeof(MarkdownViewer),
            new(MdImageLoader.Instance, OnContainersSourceChanged));

    /// <summary>Адрес документа: от него разбираются относительные адреса картинок.</summary>
    public static readonly DependencyProperty BaseUriProperty =
        DependencyProperty.Register(
            nameof(BaseUri),
            typeof(Uri),
            typeof(MarkdownViewer),
            new(null, OnContainersSourceChanged));

    /// <summary>Кнопка копирования в блоках кода, видна при наведении; наследуется блоками кода.</summary>
    public static readonly DependencyProperty ShowCodeCopyButtonProperty =
        DependencyProperty.RegisterAttached(
            nameof(ShowCodeCopyButton),
            typeof(bool),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Копировать как markdown: разметка выделенного обычным текстом; Ctrl+Shift+C.</summary>
    public static readonly RoutedUICommand CopyMarkdownCommand =
        new(
            "Copy as Markdown",
            nameof(CopyMarkdownCommand),
            typeof(MarkdownViewer),
            new() { new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift) });

    /// <summary>Номера строк исходника на полях слева.</summary>
    public static readonly DependencyProperty ShowSourceLineNumbersProperty =
        DependencyProperty.Register(
            nameof(ShowSourceLineNumbers),
            typeof(bool),
            typeof(MarkdownViewer),
            new(false));

    /// <summary>Номера строк внутри блоков кода; наследуется блоками кода.</summary>
    public static readonly DependencyProperty ShowCodeLineNumbersProperty =
        DependencyProperty.RegisterAttached(
            nameof(ShowCodeLineNumbers),
            typeof(bool),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    private static readonly DependencyPropertyKey SelectionStartPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(SelectionStart),
            typeof(int),
            typeof(MarkdownViewer),
            new(0));

    /// <summary>Начало выделения в текстовой карте.</summary>
    public static readonly DependencyProperty SelectionStartProperty = SelectionStartPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey SelectionLengthPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(SelectionLength),
            typeof(int),
            typeof(MarkdownViewer),
            new(0));

    /// <summary>Длина выделения в текстовой карте.</summary>
    public static readonly DependencyProperty SelectionLengthProperty = SelectionLengthPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey TextMapPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(TextMap),
            typeof(MdTextMap),
            typeof(MarkdownViewer),
            new(MdTextMap.Empty));

    /// <summary>Текстовая карта показанного документа; меняется с каждым новым текстом.</summary>
    public static readonly DependencyProperty TextMapProperty = TextMapPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey FrontMatterPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(FrontMatter),
            typeof(string),
            typeof(MarkdownViewer),
            new(null));

    /// <summary>Текст front matter документа без разбора; нет его — <c>null</c>.</summary>
    public static readonly DependencyProperty FrontMatterProperty = FrontMatterPropertyKey.DependencyProperty;


    private const string GutterPart = "PART_Gutter";
    private const string RowMarksPart = "PART_RowMarks";

    private static readonly DependencyProperty FeedQueryProperty =
        MdFeedSearch.QueryProperty.AddOwner(
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnFeedSearchChanged));

    private static readonly DependencyProperty FeedMatchCaseProperty =
        MdFeedSearch.MatchCaseProperty.AddOwner(
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnFeedSearchChanged));

    private static readonly DependencyProperty FeedWholeWordProperty =
        MdFeedSearch.WholeWordProperty.AddOwner(
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnFeedSearchChanged));

    private readonly SourcePositions _positions;
    private readonly WidgetStates _states;
    private readonly ModelIdentity _identity = new();
    private readonly ModelCollection<BlockVm> _blocks = [];
    private readonly MdMarkLayer _decorationLayer = new("Decorations");
    private readonly ThemeBrushes _themeBrushes;
    private IReadOnlyList<MarkSet> _markSets = [];
    private readonly MdMarkLayer _feedLayer = new("Feed search");
    private bool _updatingMarks;
    private IReadOnlyList<MdExtension> _activeExtensions = [];
    private MarkdownPipeline? _pipeline;
    private bool _templatesChanged;
    private bool _viewportNotificationQueued;
    private (Func<bool> Step, int Attempts)? _pendingScroll;

    static MarkdownViewer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MarkdownViewer), new FrameworkPropertyMetadata(typeof(MarkdownViewer)));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownViewer),
            new(
                ApplicationCommands.Copy,
                (sender, _) => ((MarkdownViewer)sender).Copy(),
                (sender, e) => e.CanExecute = sender is MarkdownViewer { IsCopyEnabled: true, SelectionLength: > 0 }));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownViewer),
            new(
                CopyMarkdownCommand,
                (sender, _) => ((MarkdownViewer)sender).CopyMarkdown(),
                (sender, e) => e.CanExecute = sender is MarkdownViewer { IsCopyEnabled: true, SelectionLength: > 0 }));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownViewer),
            new(ApplicationCommands.SelectAll, (sender, _) => ((MarkdownViewer)sender).SelectAll()));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownViewer),
            new(
                MarkdownLink.ActivateCommand,
                (sender, e) => MarkdownLink.Activate(ElementOf(e.OriginalSource) ?? (UIElement)sender, (string)e.Parameter),
                (sender, e) => e.CanExecute = e.Parameter is string { Length: > 0 }));
    }

    public MarkdownViewer()
    {
        _states = new(() => ActiveExtensions);
        SetValue(MdWidget.StatesProperty, _states);

        _themeBrushes = new(AddLogicalChild);
        _themeBrushes.Changed += RebuildMarks;
        _decorationLayer.Changed += OnMarkLayerChanged;
        _feedLayer.Changed += OnMarkLayerChanged;
        MarkLayers.CollectionChanged += OnMarkLayersChanged;
        Extensions.CollectionChanged += OnExtensionsCollectionChanged;
        ItemsSource = _blocks;

        Selection = new(this);
        Selection.Changed += OnSelectionChanged;
        _positions = new(this);

        // BYDESIGN: фокус по щелчку нужен ради копирования; показывать при этом вьюер целиком
        // внешняя прокрутка не должна.
        AddHandler(RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler((_, e) => e.Handled = ReferenceEquals(e.TargetObject, this)));
        AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((_, _) => OnViewportMaybeChanged()));
    }

    /// <summary>Выделение изменилось.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Раскладка видимой части могла смениться: прокрутка снаружи и внутри блоков, уточнились высоты, новый текст.</summary>
    public event EventHandler? ViewportChanged;

    /// <summary>Новый текст лёг в документ: прежняя карта и новая.</summary>
    internal event Action<MdTextMap, MdTextMap>? TextMapChanged;

    public string? Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public MarkdownPipeline? MarkdownPipeline
    {
        get => (MarkdownPipeline?)GetValue(MarkdownPipelineProperty);
        set => SetValue(MarkdownPipelineProperty, value);
    }

    public Action<MarkdownPipelineBuilder>? PipelineSetup
    {
        get => (Action<MarkdownPipelineBuilder>?)GetValue(PipelineSetupProperty);
        set => SetValue(PipelineSetupProperty, value);
    }

    public object? DocumentKey
    {
        get => GetValue(DocumentKeyProperty);
        set => SetValue(DocumentKeyProperty, value);
    }

    public IMdStyleProvider? StyleProvider
    {
        get => (IMdStyleProvider?)GetValue(StyleProviderProperty);
        set => SetValue(StyleProviderProperty, value);
    }

    /// <summary>Свои расширения вьюера; опрашиваются раньше общих.</summary>
    public MdExtensionCollection Extensions { get; } = [];

    public MdExtensionCollection? SharedExtensions
    {
        get => (MdExtensionCollection?)GetValue(SharedExtensionsProperty);
        set => SetValue(SharedExtensionsProperty, value);
    }

    public IReadOnlyList<MdDecoration>? Decorations
    {
        get => (IReadOnlyList<MdDecoration>?)GetValue(DecorationsProperty);
        set => SetValue(DecorationsProperty, value);
    }

    /// <summary>Слои пометок в порядке рисования: над декорациями, под выделением.</summary>
    public ObservableCollection<MdMarkLayer> MarkLayers { get; } = [];

    public IReadOnlyList<MdGap>? Gaps
    {
        get => (IReadOnlyList<MdGap>?)GetValue(GapsProperty);
        set => SetValue(GapsProperty, value);
    }

    public Brush? SelectionBrush
    {
        get => (Brush?)GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    public bool IsSelectionEnabled
    {
        get => (bool)GetValue(IsSelectionEnabledProperty);
        set => SetValue(IsSelectionEnabledProperty, value);
    }

    public bool IsCopyEnabled
    {
        get => (bool)GetValue(IsCopyEnabledProperty);
        set => SetValue(IsCopyEnabledProperty, value);
    }

    public IMdImageLoader? ImageLoader
    {
        get => (IMdImageLoader?)GetValue(ImageLoaderProperty);
        set => SetValue(ImageLoaderProperty, value);
    }

    public Uri? BaseUri
    {
        get => (Uri?)GetValue(BaseUriProperty);
        set => SetValue(BaseUriProperty, value);
    }

    public bool ShowSourceLineNumbers
    {
        get => (bool)GetValue(ShowSourceLineNumbersProperty);
        set => SetValue(ShowSourceLineNumbersProperty, value);
    }

    public bool ShowCodeLineNumbers
    {
        get => (bool)GetValue(ShowCodeLineNumbersProperty);
        set => SetValue(ShowCodeLineNumbersProperty, value);
    }

    public bool ShowCodeCopyButton
    {
        get => (bool)GetValue(ShowCodeCopyButtonProperty);
        set => SetValue(ShowCodeCopyButtonProperty, value);
    }

    public MdTextMap TextMap
    {
        get => (MdTextMap)GetValue(TextMapProperty);
        private set => SetValue(TextMapPropertyKey, value);
    }

    public string? FrontMatter => (string?)GetValue(FrontMatterProperty);

    public int SelectionStart => (int)GetValue(SelectionStartProperty);

    public int SelectionLength => (int)GetValue(SelectionLengthProperty);

    /// <summary>Выделенный текст для копирования; считается по запросу.</summary>
    public string SelectedText => TextMap.GetText(Selection.Range);

    /// <summary>Выделенное, записанное markdown со своим оформлением; считается по запросу.</summary>
    public string SelectedMarkdown => MdMarkdownWriter.Write(_blocks, TextMap, Markdown ?? string.Empty, Selection.Range);

    /// <summary>Свои расширения, следом общие — в порядке опроса.</summary>
    internal IReadOnlyList<MdExtension> ActiveExtensions => _activeExtensions;

    internal SelectionController Selection { get; }

    internal MdBlockPanel? BlockPanel { get; set; }

    internal MdLineGutter? Gutter { get; private set; }

    internal MdRowMarks? RowMarks { get; private set; }

    // BYDESIGN: кисти пометок — логические потомки вьюера: иначе смена темы до них не доходит.
    protected override IEnumerator LogicalChildren => WithThemeBrushes(base.LogicalChildren, _themeBrushes.All);

    public static MdExtensionCollection? GetSharedExtensions(DependencyObject element)
        => (MdExtensionCollection?)element.GetValue(SharedExtensionsProperty);

    public static void SetSharedExtensions(DependencyObject element, MdExtensionCollection? value)
        => element.SetValue(SharedExtensionsProperty, value);

    public static bool GetShowCodeCopyButton(DependencyObject element)
        => (bool)element.GetValue(ShowCodeCopyButtonProperty);

    public static void SetShowCodeCopyButton(DependencyObject element, bool value)
        => element.SetValue(ShowCodeCopyButtonProperty, value);

    public static bool GetShowCodeLineNumbers(DependencyObject element)
        => (bool)element.GetValue(ShowCodeLineNumbersProperty);

    public static void SetShowCodeLineNumbers(DependencyObject element, bool value)
        => element.SetValue(ShowCodeLineNumbersProperty, value);

    /// <summary>Позиция строки исходника в координатах вьюера; дробная часть — доля строки.</summary>
    public double? GetSourceLineOffset(double line) => _positions.OffsetOf(line);

    /// <summary>Строка исходника в позиции вьюера; дробная часть — доля строки.</summary>
    public double? GetSourceLineAt(double offset) => _positions.LineAt(offset);

    /// <summary>Где в координатах вьюера стояли бы пустые места строк, не будь у вьюера мест; строки — по возрастанию.</summary>
    internal double?[] GetGapOffsets(IReadOnlyList<int> lines) => _positions.GapOffsetsOf(lines);

    /// <summary>
    /// Строка исходника, которая видна в доле <paramref name="viewportFraction"/> высоты прокрутки,
    /// в которой стоит вьюер: 0 — верхний край, 1 — нижний.
    /// </summary>
    public double? GetVisibleSourceLine(double viewportFraction = 0)
    {
        if (Scrollers.Nearest(this) is not { } scroller)
        {
            return null;
        }

        var point = scroller.TranslatePoint(new(0, viewportFraction * scroller.ViewportHeight), this);
        return GetSourceLineAt(point.Y);
    }

    /// <summary>
    /// Прокручивает прокрутку, в которой стоит вьюер, так, чтобы строка исходника встала в долю
    /// <paramref name="viewportFraction"/> её высоты. Прокрутка должна быть попиксельной.
    /// </summary>
    public bool ScrollToSourceLine(double line, double viewportFraction = 0)
        => StartScroll(() => ScrollToSourceLineOnce(line, viewportFraction));

    /// <summary>Прокрутка, в которой стоит вьюер.</summary>
    ScrollViewer? IMdScrollSide.Scroller => Scrollers.Nearest(this);

    /// <summary>
    /// Прокручивает прокрутку, в которой стоит вьюер, так, чтобы начало куска текстовой карты встало
    /// в долю <paramref name="viewportFraction"/> её высоты; кусок может лежать и в несозданных блоках.
    /// </summary>
    public bool ScrollToRange(MdTextRange range, double viewportFraction = 0)
        => StartScroll(() => ScrollToRangeOnce(range, viewportFraction));

    /// <summary>
    /// Прокручивает к якорю документа — заголовку или сноске — так же, как <see cref="ScrollToRange"/>;
    /// якоря нет — <c>false</c>.
    /// </summary>
    public bool ScrollToAnchor(string id, double viewportFraction = 0)
        => (TextMap.TryGetAnchor(id, out var range) || TextMap.TryGetAnchor(Uri.UnescapeDataString(id), out range))
           && ScrollToRange(range, viewportFraction);

    /// <summary>
    /// Позиция в текстовой карте для точки в координатах вьюера; с <paramref name="snapToText"/> —
    /// у ближайшего текста, без него вне текста — <c>null</c>.
    /// </summary>
    public int? GetPositionFromPoint(Point point, bool snapToText) => Selection.FindOffset(point, snapToText);

    /// <summary>Прямоугольники куска текстовой карты в координатах вьюера; есть только у созданных блоков.</summary>
    public IReadOnlyList<Rect> GetRangeRects(MdTextRange range)
    {
        var rects = new List<Rect>();
        if (range.IsEmpty)
        {
            return rects;
        }

        foreach (var target in SelectionController.Targets(this))
        {
            switch (target)
            {
                case MdTextBlock text when text.TryGetMapRange(out var own) && own.Intersect(range) is { IsEmpty: false } common:
                    var toViewer = text.TransformToAncestor(this);
                    rects.AddRange(text.GetBounds(common.Start - own.Start, common.End - own.Start).Select(toViewer.TransformBounds));
                    break;

                case MdBlockItem item when item.TryGetAtomicRange(out var own) && !own.Intersect(range).IsEmpty:
                    rects.Add(item.TransformToAncestor(this).TransformBounds(new(item.RenderSize)));
                    break;
            }
        }

        return rects;
    }

    /// <summary>Рамка куска текстовой карты в координатах вьюера; <see cref="Rect.Empty"/> — кусок не на созданных блоках.</summary>
    public Rect GetRangeBounds(MdTextRange range) => GetRangeRects(range).Aggregate(Rect.Empty, Rect.Union);

    /// <summary>Геометрия документа сразу в координатах элемента: куски текста, строки исходника, точки.</summary>
    public MdCoordinates GetCoordinates(UIElement relativeTo) => new(this, relativeTo);

    /// <summary>
    /// Пометки под точкой вьюера в порядке рисования: над текстом — пометки текста под ней и строки
    /// во всю ширину на её строке, на полях — полосы и значки её строки.
    /// </summary>
    public IReadOnlyList<(MdMarkLayer Layer, MdMark Mark)> GetMarksAt(Point point)
    {
        var hits = new List<(MdMarkLayer Layer, MdMark Mark)>();
        var onMargin = Gutter is { IsVisible: true } gutter && new Rect(gutter.RenderSize).Contains(TranslatePoint(point, gutter));
        var position = onMargin ? null : GetPositionFromPoint(point, snapToText: false);
        var line = GetSourceLineAt(point.Y) is { } at ? (int)Math.Floor(at) : (int?)null;

        foreach (var set in _markSets)
        {
            if (position is { } offset)
            {
                hits.AddRange(set
                    .Within(new MdTextRange(Math.Max(0, offset - 1), 2))
                    .Where(mark => GetRangeRects(mark.Range).Any(rect => rect.Contains(point)))
                    .Select(mark => (set.Layer, mark.Mark)));
            }

            if (line is { } row)
            {
                hits.AddRange(set
                    .Within(row, row)
                    .Where(mark => onMargin ? mark.Mark.Kind is MdMarkKind.Bar or MdMarkKind.Icon : mark.Mark.Kind == MdMarkKind.Row)
                    .Select(mark => (set.Layer, mark.Mark)));
            }
        }

        return hits;
    }

    /// <summary>Пометки в позиции текстовой карты в порядке рисования: куски текста, где она стоит, и строки её строки исходника.</summary>
    public IReadOnlyList<(MdMarkLayer Layer, MdMark Mark)> GetMarksAt(int position)
    {
        var hits = new List<(MdMarkLayer Layer, MdMark Mark)>();
        var line = TextMap.SourceLineOf(position);

        foreach (var set in _markSets)
        {
            hits.AddRange(set.Within(new MdTextRange(position, 1)).Select(mark => (set.Layer, mark.Mark)));
            hits.AddRange(set.CaretsWithin(new MdTextRange(position, 0)).Select(mark => (set.Layer, mark.Mark)));
            if (line is { } row)
            {
                hits.AddRange(set.Within(row, row).Select(mark => (set.Layer, mark.Mark)));
            }
        }

        return hits;
    }

    /// <summary>
    /// Собирает документ заново из того же текста: расширения снова спрашивают приложение — резолвер,
    /// шаблоны ссылок, обработчики. Выделение и состояния виджетов остаются.
    /// </summary>
    public void Refresh()
    {
        _pipeline = null;
        Rebuild();
    }

    /// <summary>Выделяет кусок текстовой карты.</summary>
    public void Select(int start, int length) => Selection.Select(start, length);

    public void SelectAll() => Selection.Select(0, TextMap.Length);

    public void ClearSelection() => Selection.Select(Selection.Range.Start, 0);

    /// <summary>Кладёт выделенное в буфер обмена голым текстом и разметкой; перед этим поднимает <see cref="DataObject.CopyingEvent"/>.</summary>
    public void Copy()
    {
        if (CreateCopyData() is { } data)
        {
            MdClipboard.Put(data);
        }
    }

    /// <summary>Кладёт разметку выделенного обычным текстом — её можно вставить куда угодно; тоже через <see cref="DataObject.CopyingEvent"/>.</summary>
    public void CopyMarkdown()
    {
        if (CreateCopyData(markdownAsText: true) is { } data)
        {
            MdClipboard.Put(data);
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Gutter = GetTemplateChild(GutterPart) as MdLineGutter;
        RowMarks = GetTemplateChild(RowMarksPart) as MdRowMarks;
    }

    /// <summary>Ближайший вьюер, внутри которого стоит элемент.</summary>
    internal static MarkdownViewer? Owning(DependencyObject element)
    {
        for (DependencyObject? node = element;
             node is not null;
             node = node is Visual or Visual3D
                 ? VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node)
                 : LogicalTreeHelper.GetParent(node))
        {
            if (node is MarkdownViewer viewer)
            {
                return viewer;
            }
        }

        return null;
    }

    /// <summary>Созданные блоки встали на другие места или сменились.</summary>
    internal void OnBlocksArranged()
    {
        Gutter?.InvalidateVisual();
        RowMarks?.InvalidateVisual();
    }

    /// <summary>Строки исходника созданных блоков: от первой строки первого до последней строки последнего.</summary>
    internal (int First, int Last)? RealizedLines()
    {
        if (BlockPanel?.RealizedRange is not { } realized || ItemsSource is not IReadOnlyList<BlockVm> items)
        {
            return null;
        }

        var sources = Enumerable
            .Range(realized.First, Math.Min(realized.Last, items.Count - 1) - realized.First + 1)
            .Select(index => items[index].Source)
            .Where(source => !source.IsEmpty)
            .ToList();

        return sources.Count == 0 ? null : (sources[0].StartLine, sources[^1].EndLine);
    }

    /// <summary>
    /// Полоса строк исходника с <paramref name="first"/> по <paramref name="last"/> в координатах
    /// элемента: от верха первой до верха следующей за последней; полосы нет — <c>null</c>.
    /// </summary>
    internal (double Top, double Height)? GetLineBand(int first, int last, UIElement element)
    {
        if (GetSourceLineOffset(first) is not { } top || GetSourceLineOffset(last + 1) is not { } bottom || bottom <= top)
        {
            return null;
        }

        var from = TranslatePoint(new(0, top), element).Y;
        var to = TranslatePoint(new(0, bottom), element).Y;
        return (from, to - from);
    }

    internal void OnViewportMaybeChanged()
    {
        if (_viewportNotificationQueued || ViewportChanged is null)
        {
            return;
        }

        _viewportNotificationQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Render,
            () =>
            {
                _viewportNotificationQueued = false;
                ViewportChanged?.Invoke(this, EventArgs.Empty);
            });
    }

    /// <summary>Оба вида выделенного для буфера обмена после <see cref="DataObject.CopyingEvent"/>; <c>null</c> — копировать нечего или приложение отменило.</summary>
    internal DataObject? CreateCopyData(bool markdownAsText = false)
    {
        if (SelectionLength == 0)
        {
            return null;
        }

        var markdown = SelectedMarkdown;
        var data = MdClipboard.Data(markdownAsText ? markdown : SelectedText, markdown, MdClipboard.Html(markdown, GetPipeline()));
        return MdClipboard.Approve(this, data) ? data : null;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MdDocumentAutomationPeer(this);

    protected override DependencyObject GetContainerForItemOverride() => new MdBlockItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MdBlockItem;

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);

        if (SelectionController.IsInteractive(e.OriginalSource as DependencyObject, this))
        {
            return;
        }

        var point = e.GetPosition(this);
        var selected = e.ClickCount switch
        {
            2 => Selection.SelectWordAt(point),
            3 => Selection.SelectBlockAt(point),
            _ => Selection.Press(point, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        };

        if (selected)
        {
            Focus();
        }
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);

        if (e.LeftButton == MouseButtonState.Pressed && Selection.Drag(e.GetPosition(this)) && !IsMouseCaptured)
        {
            CaptureMouse();
        }
    }

    /// <summary>Вьюер потерял мышь посреди протаскивания — переключение окна, всплывшее окно: протаскивание и автопрокрутка кончаются.</summary>
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);

        // BYDESIGN: только своя потеря — захват, который вьюер забирает у ссылки в начале протаскивания,
        // тоже поднимает это событие, но от ссылки.
        if (ReferenceEquals(e.OriginalSource, this))
        {
            Selection.Release();
        }
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);

        if (Selection.Release())
        {
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).Rebuild();

    private static void OnPipelineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).ResetPipeline();

    private static void OnDocumentKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (MarkdownViewer)d;
        viewer._identity.Forget();
        viewer.Rebuild();
    }

    /// <summary>Сменилось то, что контейнеры берут при создании: провайдер стилей, загрузчик картинок, адрес документа.</summary>
    private static void OnContainersSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (MarkdownViewer)d;
        viewer._templatesChanged = true;
        viewer.Rebuild();
    }

    private static void OnDecorationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var decorations = (IReadOnlyList<MdDecoration>?)e.NewValue ?? [];
        ((MarkdownViewer)d)._decorationLayer.Marks = decorations
            .Select(decoration => new MdMark(decoration.Range) { Brush = decoration.Brush })
            .ToList();
    }

    private static void OnSelectionBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).UpdateLayer();

    private static void OnFeedSearchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).UpdateFeedSearch();

    private static void OnGapsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).UpdateGapLayout();

    private static IEnumerator WithThemeBrushes(IEnumerator? children, IEnumerable<ThemeBrush> brushes)
    {
        while (children?.MoveNext() == true)
        {
            yield return children.Current;
        }

        foreach (var brush in brushes)
        {
            yield return brush;
        }
    }

    private static void OnIsSelectionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).ClearSelection();

    /// <summary>Ближайший к источнику команды элемент интерфейса: у куска текста — его текстовый блок.</summary>
    private static UIElement? ElementOf(object source)
    {
        for (var node = source as DependencyObject; node is not null; node = LogicalTreeHelper.GetParent(node))
        {
            if (node is UIElement element)
            {
                return element;
            }
        }

        return null;
    }

    private static void OnSharedExtensionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MarkdownViewer viewer)
        {
            return;
        }

        // BYDESIGN: подписка слабая — общая коллекция живёт дольше вьюеров и не должна их держать.
        if (e.OldValue is MdExtensionCollection oldExtensions)
        {
            CollectionChangedEventManager.RemoveHandler(oldExtensions, viewer.OnExtensionsCollectionChanged);
        }

        if (e.NewValue is MdExtensionCollection newExtensions)
        {
            CollectionChangedEventManager.AddHandler(newExtensions, viewer.OnExtensionsCollectionChanged);
        }

        viewer.ResetPipeline();
    }

    private void OnExtensionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ResetPipeline();

    private void OnMarkLayersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var layer in e.OldItems?.OfType<MdMarkLayer>() ?? [])
        {
            layer.Changed -= OnMarkLayerChanged;
        }

        foreach (var layer in e.NewItems?.OfType<MdMarkLayer>() ?? [])
        {
            layer.Changed += OnMarkLayerChanged;
        }

        RebuildMarks();
    }

    private void OnMarkLayerChanged(object? sender, EventArgs e)
    {
        if (!_updatingMarks)
        {
            RebuildMarks();
        }
    }

    /// <summary>Раскладывает пометки всех слоёв по текущей карте и ставит новый снимок подсветки.</summary>
    private void RebuildMarks()
    {
        _markSets =
        [
            new MarkSet(_decorationLayer, TextMap, _themeBrushes.BrushOf),
            new MarkSet(_feedLayer, TextMap, _themeBrushes.BrushOf),
            .. MarkLayers.Select(layer => new MarkSet(layer, TextMap, _themeBrushes.BrushOf))
        ];
        UpdateLayer();
        Gutter?.InvalidateMeasure();
    }

    /// <summary>Раскладывает пустые места по блокам нынешней модели.</summary>
    private void UpdateGapLayout()
    {
        var gaps = Gaps ?? [];
        if (gaps.Count == 0 && GetValue(GapLayout.LayoutProperty) is null)
        {
            return;
        }

        SetValue(GapLayout.LayoutProperty, gaps.Count == 0 ? GapLayout.Empty : new GapLayout(_blocks, gaps));
    }

    /// <summary>Совпадения запроса поиска в ленте, который вьюер унаследовал от списка.</summary>
    private void UpdateFeedSearch()
    {
        var query = (string?)GetValue(FeedQueryProperty);
        if (string.IsNullOrEmpty(query) && _feedLayer.Marks.Count == 0)
        {
            return;
        }

        _feedLayer.Marks = TextMatches
            .Find(TextMap.Text, query, (bool)GetValue(FeedMatchCaseProperty), (bool)GetValue(FeedWholeWordProperty))
            .Select(range => new MdMark(range) { Style = "Found" })
            .ToList();
    }

    /// <summary>Пометки слоёв, которые следуют за текстом, отслеживаемыми местами старой карты.</summary>
    private List<(MdMarkLayer Layer, List<(MdMark Mark, MdTrackedRange? Range, MdTrackedLines? Lines)> Marks)> TrackFollowingMarks()
        => MarkLayers
            .Where(layer => layer.FollowsText && layer.Marks.Count > 0)
            .Select(layer => (layer, layer.Marks
                .Select(mark => (
                    mark,
                    mark.Range is { } range ? TextMap.Track(range) : null,
                    mark.Lines is { } lines ? TextMap.TrackSourceLines(lines.Start, lines.End) : null))
                .ToList()))
            .ToList();

    /// <summary>Пометка на месте своего отслеживаемого места в новой карте; место пропало — <c>null</c>.</summary>
    private MdMark? Moved((MdMark Mark, MdTrackedRange? Range, MdTrackedLines? Lines) tracked)
    {
        if (tracked.Range is { } range)
        {
            return TextMap.Find(range) is { } found && (!found.IsEmpty || tracked.Mark.Kind == MdMarkKind.Caret)
                ? tracked.Mark with { Range = found }
                : null;
        }

        if (tracked.Lines is { } lines)
        {
            return TextMap.Find(lines) is { } span ? tracked.Mark with { Lines = span } : null;
        }

        return tracked.Mark;
    }

    private void OnSelectionChanged()
    {
        var range = Selection.Range;
        var moved = range.Start != SelectionStart || range.Length != SelectionLength;
        var wasEmpty = SelectionLength == 0;
        SetValue(SelectionStartPropertyKey, range.Start);
        SetValue(SelectionLengthPropertyKey, range.Length);
        UpdateLayer();

        if (wasEmpty != range.IsEmpty)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        if (moved)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool ScrollToSourceLineOnce(double line, double viewportFraction)
    {
        if (Scrollers.Nearest(this) is not { } scroller || GetSourceLineOffset(line) is not { } offset)
        {
            return false;
        }

        ScrollTo(scroller, offset, viewportFraction);
        return true;
    }

    /// <summary>Созданное начало куска — точно по его прямоугольнику, несозданное — по строке исходника.</summary>
    private bool ScrollToRangeOnce(MdTextRange range, double viewportFraction)
    {
        if (Scrollers.Nearest(this) is not { } scroller)
        {
            return false;
        }

        if (GetRangeRects(new(range.Start, 1)) is [var head, ..])
        {
            ScrollTo(scroller, head.Top, viewportFraction);
            return true;
        }

        return TextMap.SourceLineOf(range.Start) is { } line && ScrollToSourceLineOnce(line, viewportFraction);
    }

    /// <summary>Сдвигает прокрутку так, чтобы позиция вьюера встала в долю её высоты.</summary>
    private void ScrollTo(ScrollViewer scroller, double offset, double viewportFraction)
    {
        var position = TranslatePoint(new(0, offset), scroller).Y;
        var shift = position - viewportFraction * scroller.ViewportHeight;
        if (Math.Abs(shift) >= 0.5)
        {
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset + shift);
        }
    }

    private bool StartScroll(Func<bool> step)
    {
        if (!step())
        {
            return false;
        }

        _pendingScroll = (step, 3);
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, RefineScroll);
        return true;
    }

    /// <summary>
    /// После прокрутки созданные блоки уточняют свои высоты, и цель съезжает — прокрутка
    /// повторяется, пока цель не встанет на место.
    /// </summary>
    private void RefineScroll()
    {
        if (_pendingScroll is not { } pending)
        {
            return;
        }

        _pendingScroll = null;
        if (pending.Attempts > 0 && pending.Step())
        {
            _pendingScroll = pending with { Attempts = pending.Attempts - 1 };
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, RefineScroll);
        }
    }

    private void UpdateLayer()
    {
        SetValue(MdSelection.LayerProperty, new MdDecorationLayer(TextMap, Selection?.Range ?? default, SelectionBrush, _markSets, IsSelectionEnabled));
    }

    private void ResetPipeline()
    {
        _activeExtensions = [.. Extensions, .. SharedExtensions ?? []];
        _pipeline = null;
        _templatesChanged = true;
        _states.Clear();
        Rebuild();
    }

    private MarkdownPipeline GetPipeline()
    {
        if (_pipeline is not null)
        {
            return _pipeline;
        }

        var extensions = ActiveExtensions;
        if (PipelineSetup is null && extensions.Count == 0)
        {
            return _pipeline = MarkdownPipeline ?? ModelBuilder.DefaultPipeline;
        }

        var builder = new MarkdownPipelineBuilder();
        if (PipelineSetup is { } setup)
        {
            setup(builder);
        }
        else
        {
            // BYDESIGN: из готового конвейера достаются только расширения Markdig — настройки
            // построителя он не отдаёт; для них есть PipelineSetup.
            foreach (var markdigExtension in (MarkdownPipeline ?? ModelBuilder.DefaultPipeline).Extensions)
            {
                builder.Extensions.Add(markdigExtension);
            }
        }

        foreach (var extension in extensions)
        {
            extension.SetupPipeline(builder);
        }

        return _pipeline = builder.Build();
    }

    /// <summary>
    /// Новый текст ложится правками: блок с прежним идентификатором остаётся в своём контейнере.
    /// Сменились шаблоны — контейнеры создаются заново. Выделение находит своё место в новом тексте,
    /// как отслеживаемый кусок; его блок или текст исчез — выделение снимается.
    /// </summary>
    private void Rebuild()
    {
        var old = TextMap;
        var selected = Selection is { Range.IsEmpty: false } selection
            ? (Range: TextMap.Track(selection.Range), Backward: selection.Ends.Active < selection.Ends.Anchor)
            : default;
        var following = TrackFollowingMarks();

        string? frontMatter = null;
        var blocks = StyleProvider is null
            ? Array.Empty<BlockVm>()
            : ModelBuilder.Parse(Markdown ?? string.Empty, GetPipeline(), ActiveExtensions, _identity, out frontMatter);

        TextMap = MdTextMap.Build(blocks, Markdown ?? string.Empty);
        SetValue(FrontMatterPropertyKey, frontMatter);
        _states.Retain(_identity.Contains);
        _blocks.Apply(blocks);
        UpdateGapLayout();

        if (_templatesChanged)
        {
            _templatesChanged = false;
            Items.Refresh();
        }

        if (selected.Range is { } tracked && TextMap.Find(tracked) is { IsEmpty: false } range)
        {
            Selection!.Restore(selected.Backward ? range.End : range.Start, selected.Backward ? range.Start : range.End);
        }
        else
        {
            Selection?.Reset();
        }

        _updatingMarks = true;
        try
        {
            foreach (var (layer, marks) in following)
            {
                layer.Marks = marks.Select(Moved).OfType<MdMark>().ToList();
            }

            UpdateFeedSearch();
            TextMapChanged?.Invoke(old, TextMap);
        }
        finally
        {
            _updatingMarks = false;
        }

        RebuildMarks();
        Gutter?.InvalidateVisual();
        RowMarks?.InvalidateVisual();
    }
}