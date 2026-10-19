namespace Beresta.Markdown.Wpf;

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

/// <summary>A control that shows Markdown text as blocks drawn by the templates of the theme.</summary>
[TemplatePart(Name = GutterPart, Type = typeof(MdLineGutter))]
[TemplatePart(Name = RowMarksPart, Type = typeof(MdRowMarks))]
public sealed class MarkdownViewer : ItemsControl, IMdScrollSide
{
    /// <summary>The text of the document.</summary>
    public static readonly DependencyProperty MarkdownProperty =
        DependencyProperty.Register(
            nameof(Markdown),
            typeof(string),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.None, OnMarkdownChanged));

    /// <summary>
    /// A ready Markdig pipeline; not set — the extended default one. With extensions of the viewer
    /// only its Markdig extensions are used; builder settings go through
    /// <see cref="PipelineSetup"/>.
    /// </summary>
    public static readonly DependencyProperty MarkdownPipelineProperty =
        DependencyProperty.Register(
            nameof(MarkdownPipeline),
            typeof(MarkdownPipeline),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(null, OnPipelineChanged));

    /// <summary>
    /// A setup of a fresh Markdig builder, with the extensions of the viewer on top of it; when
    /// set, it is used instead of <see cref="MarkdownPipeline"/>.
    /// </summary>
    public static readonly DependencyProperty PipelineSetupProperty =
        DependencyProperty.Register(
            nameof(PipelineSetup),
            typeof(Action<MarkdownPipelineBuilder>),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(null, OnPipelineChanged));

    /// <summary>
    /// The key of the document: when it changes, the text is another document, and the selection,
    /// collapsed blocks and other widget states do not move into it.
    /// </summary>
    public static readonly DependencyProperty DocumentKeyProperty =
        DependencyProperty.Register(
            nameof(DocumentKey),
            typeof(object),
            typeof(MarkdownViewer),
            new(null, OnDocumentKeyChanged));

    /// <summary>
    /// The length of a text in characters from which it is parsed outside the interface thread; 0 —
    /// always on it.
    /// </summary>
    public static readonly DependencyProperty BackgroundParseThresholdProperty =
        DependencyProperty.Register(
            nameof(BackgroundParseThreshold),
            typeof(int),
            typeof(MarkdownViewer),
            new(0));

    /// <summary>The templates of built-in blocks.</summary>
    public static readonly DependencyProperty StyleProviderProperty =
        DependencyProperty.Register(
            nameof(StyleProvider),
            typeof(IMdStyleProvider),
            typeof(MarkdownViewer),
            new(null, OnContainersSourceChanged));

    /// <summary>
    /// Extensions shared by several viewers; set on a window, a panel or in a style, they are
    /// inherited by the viewers inside.
    /// </summary>
    public static readonly DependencyProperty SharedExtensionsProperty =
        DependencyProperty.RegisterAttached(
            nameof(SharedExtensions),
            typeof(MdExtensionCollection),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnSharedExtensionsChanged));

    /// <summary>
    /// Highlights of pieces of text under the mark layers and the selection: found text, changes of
    /// a diff.
    /// </summary>
    public static readonly DependencyProperty DecorationsProperty =
        DependencyProperty.Register(
            nameof(Decorations),
            typeof(IReadOnlyList<MdDecoration>),
            typeof(MarkdownViewer),
            new(null, OnDecorationsChanged));

    /// <summary>Empty places before source lines — for example, to keep the two panes of a diff level.</summary>
    public static readonly DependencyProperty GapsProperty =
        DependencyProperty.Register(
            nameof(Gaps),
            typeof(IReadOnlyList<MdGap>),
            typeof(MarkdownViewer),
            new(null, OnGapsChanged));

    /// <summary>The brush of the selection; drawn over the text and the marks.</summary>
    public static readonly DependencyProperty SelectionBrushProperty =
        DependencyProperty.Register(
            nameof(SelectionBrush),
            typeof(Brush),
            typeof(MarkdownViewer),
            new(null, OnSelectionBrushChanged));

    /// <summary>Whether text can be selected; turning it off clears the selection.</summary>
    public static readonly DependencyProperty IsSelectionEnabledProperty =
        DependencyProperty.Register(
            nameof(IsSelectionEnabled),
            typeof(bool),
            typeof(MarkdownViewer),
            new(true, OnIsSelectionEnabledChanged));

    /// <summary>
    /// Whether the viewer handles Ctrl+C itself; when off, it leaves the copy command to the
    /// application.
    /// </summary>
    public static readonly DependencyProperty IsCopyEnabledProperty =
        DependencyProperty.Register(
            nameof(IsCopyEnabled),
            typeof(bool),
            typeof(MarkdownViewer),
            new(true, (_, _) => CommandManager.InvalidateRequerySuggested()));

    /// <summary>Where images come from; by default <see cref="MdImageLoader"/>.</summary>
    public static readonly DependencyProperty ImageLoaderProperty =
        DependencyProperty.Register(
            nameof(ImageLoader),
            typeof(IMdImageLoader),
            typeof(MarkdownViewer),
            new(MdImageLoader.Instance, OnContainersSourceChanged));

    /// <summary>Which images to load; inherited. By default <see cref="MdImagePolicy.Default"/>.</summary>
    public static readonly DependencyProperty ImagePolicyProperty =
        DependencyProperty.RegisterAttached(
            nameof(ImagePolicy),
            typeof(MdImagePolicy),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(MdImagePolicy.Default, FrameworkPropertyMetadataOptions.Inherits, OnImagePolicyChanged));

    /// <summary>The address of the document: relative addresses of images are resolved against it.</summary>
    public static readonly DependencyProperty BaseUriProperty =
        DependencyProperty.Register(
            nameof(BaseUri),
            typeof(Uri),
            typeof(MarkdownViewer),
            new(null, OnContainersSourceChanged));

    /// <summary>The copy button of code blocks, shown on hover; inherited by code blocks.</summary>
    public static readonly DependencyProperty ShowCodeCopyButtonProperty =
        DependencyProperty.RegisterAttached(
            nameof(ShowCodeCopyButton),
            typeof(bool),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Copy as Markdown: the markup of the selection as plain text; Ctrl+Shift+C.</summary>
    public static readonly RoutedUICommand CopyMarkdownCommand =
        new(
            "Copy as Markdown",
            nameof(CopyMarkdownCommand),
            typeof(MarkdownViewer),
            new() { new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift) });

    /// <summary>Numbers of source lines in the left margin.</summary>
    public static readonly DependencyProperty ShowSourceLineNumbersProperty =
        DependencyProperty.Register(
            nameof(ShowSourceLineNumbers),
            typeof(bool),
            typeof(MarkdownViewer),
            new(false));

    /// <summary>Line numbers inside code blocks; inherited by code blocks.</summary>
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

    /// <summary>The start of the selection in the text map.</summary>
    public static readonly DependencyProperty SelectionStartProperty = SelectionStartPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey SelectionLengthPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(SelectionLength),
            typeof(int),
            typeof(MarkdownViewer),
            new(0));

    /// <summary>The length of the selection in the text map.</summary>
    public static readonly DependencyProperty SelectionLengthProperty = SelectionLengthPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey TextMapPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(TextMap),
            typeof(MdTextMap),
            typeof(MarkdownViewer),
            new(MdTextMap.Empty));

    /// <summary>The text map of the shown document; changes with every new text.</summary>
    public static readonly DependencyProperty TextMapProperty = TextMapPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey FrontMatterPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(FrontMatter),
            typeof(string),
            typeof(MarkdownViewer),
            new(null));

    /// <summary>The text of the front matter of the document, unparsed; none — <c>null</c>.</summary>
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
    private List<MdMarkLayer> _subscribedLayers = [];
    private readonly MdMarkLayer _feedLayer = new("Feed search");
    private bool _updatingMarks;
    private IReadOnlyList<MdExtension> _activeExtensions = [];
    private IReadOnlyDictionary<object, MdExtension> _owners = new Dictionary<object, MdExtension>();
    private MarkdownPipeline? _pipeline;
    private Task<ParsedDocument>? _parsing;
    private bool _rebuildQueued;
    private bool _discardRunning;
    private bool _forgetQueued;
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
            new(
                ApplicationCommands.SelectAll,
                (sender, _) => ((MarkdownViewer)sender).SelectAll(),
                (sender, e) => e.CanExecute = sender is MarkdownViewer { IsSelectionEnabled: true }));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownViewer),
            new(
                MarkdownLink.ActivateCommand,
                (sender, e) => MarkdownLink.Activate(ElementOf(e.OriginalSource) ?? (UIElement)sender, (string)e.Parameter),
                (sender, e) => e.CanExecute = e.Parameter is string { Length: > 0 }));
    }

    /// <summary>Creates an empty viewer.</summary>
    public MarkdownViewer()
    {
        Guard = new(
            (exception, extension) => Dispatcher.BeginInvoke(() => RenderFailed?.Invoke(this, new(exception, extension, isBackground: false))),
            widget => _owners.GetValueOrDefault(widget));
        _states = new(() => ActiveExtensions, Guard);
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

    /// <summary>The selection changed.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// The layout of the visible part may have changed: scrolling outside and inside blocks,
    /// refined heights, a new text.
    /// </summary>
    public event EventHandler? ViewportChanged;

    /// <summary>
    /// Markdig or an extension threw while the viewer built or drew the document; the viewer caught the
    /// exception and showed what it could.
    /// </summary>
    public event EventHandler<MdRenderFailedEventArgs>? RenderFailed;

    /// <summary>Новый текст лёг в документ: прежняя карта и новая.</summary>
    internal event Action<MdTextMap, MdTextMap>? TextMapChanged;

    /// <inheritdoc cref="MarkdownProperty"/>
    public string? Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    /// <inheritdoc cref="MarkdownPipelineProperty"/>
    public MarkdownPipeline? MarkdownPipeline
    {
        get => (MarkdownPipeline?)GetValue(MarkdownPipelineProperty);
        set => SetValue(MarkdownPipelineProperty, value);
    }

    /// <inheritdoc cref="PipelineSetupProperty"/>
    public Action<MarkdownPipelineBuilder>? PipelineSetup
    {
        get => (Action<MarkdownPipelineBuilder>?)GetValue(PipelineSetupProperty);
        set => SetValue(PipelineSetupProperty, value);
    }

    /// <inheritdoc cref="BackgroundParseThresholdProperty"/>
    public int BackgroundParseThreshold
    {
        get => (int)GetValue(BackgroundParseThresholdProperty);
        set => SetValue(BackgroundParseThresholdProperty, value);
    }

    /// <inheritdoc cref="DocumentKeyProperty"/>
    public object? DocumentKey
    {
        get => GetValue(DocumentKeyProperty);
        set => SetValue(DocumentKeyProperty, value);
    }

    /// <inheritdoc cref="StyleProviderProperty"/>
    public IMdStyleProvider? StyleProvider
    {
        get => (IMdStyleProvider?)GetValue(StyleProviderProperty);
        set => SetValue(StyleProviderProperty, value);
    }

    /// <summary>The own extensions of the viewer; asked before the shared ones.</summary>
    public MdExtensionCollection Extensions { get; } = [];

    /// <inheritdoc cref="SharedExtensionsProperty"/>
    public MdExtensionCollection? SharedExtensions
    {
        get => (MdExtensionCollection?)GetValue(SharedExtensionsProperty);
        set => SetValue(SharedExtensionsProperty, value);
    }

    /// <inheritdoc cref="DecorationsProperty"/>
    public IReadOnlyList<MdDecoration>? Decorations
    {
        get => (IReadOnlyList<MdDecoration>?)GetValue(DecorationsProperty);
        set => SetValue(DecorationsProperty, value);
    }

    /// <summary>Mark layers in drawing order: over the decorations, under the selection.</summary>
    public ObservableCollection<MdMarkLayer> MarkLayers { get; } = [];

    /// <inheritdoc cref="GapsProperty"/>
    public IReadOnlyList<MdGap>? Gaps
    {
        get => (IReadOnlyList<MdGap>?)GetValue(GapsProperty);
        set => SetValue(GapsProperty, value);
    }

    /// <inheritdoc cref="SelectionBrushProperty"/>
    public Brush? SelectionBrush
    {
        get => (Brush?)GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    /// <inheritdoc cref="IsSelectionEnabledProperty"/>
    public bool IsSelectionEnabled
    {
        get => (bool)GetValue(IsSelectionEnabledProperty);
        set => SetValue(IsSelectionEnabledProperty, value);
    }

    /// <inheritdoc cref="IsCopyEnabledProperty"/>
    public bool IsCopyEnabled
    {
        get => (bool)GetValue(IsCopyEnabledProperty);
        set => SetValue(IsCopyEnabledProperty, value);
    }

    /// <inheritdoc cref="ImageLoaderProperty"/>
    public IMdImageLoader? ImageLoader
    {
        get => (IMdImageLoader?)GetValue(ImageLoaderProperty);
        set => SetValue(ImageLoaderProperty, value);
    }

    /// <inheritdoc cref="ImagePolicyProperty"/>
    public MdImagePolicy ImagePolicy
    {
        get => (MdImagePolicy)GetValue(ImagePolicyProperty);
        set => SetValue(ImagePolicyProperty, value);
    }

    /// <inheritdoc cref="BaseUriProperty"/>
    public Uri? BaseUri
    {
        get => (Uri?)GetValue(BaseUriProperty);
        set => SetValue(BaseUriProperty, value);
    }

    /// <inheritdoc cref="ShowSourceLineNumbersProperty"/>
    public bool ShowSourceLineNumbers
    {
        get => (bool)GetValue(ShowSourceLineNumbersProperty);
        set => SetValue(ShowSourceLineNumbersProperty, value);
    }

    /// <inheritdoc cref="ShowCodeLineNumbersProperty"/>
    public bool ShowCodeLineNumbers
    {
        get => (bool)GetValue(ShowCodeLineNumbersProperty);
        set => SetValue(ShowCodeLineNumbersProperty, value);
    }

    /// <inheritdoc cref="ShowCodeCopyButtonProperty"/>
    public bool ShowCodeCopyButton
    {
        get => (bool)GetValue(ShowCodeCopyButtonProperty);
        set => SetValue(ShowCodeCopyButtonProperty, value);
    }

    /// <inheritdoc cref="TextMapProperty"/>
    public MdTextMap TextMap
    {
        get => (MdTextMap)GetValue(TextMapProperty);
        private set => SetValue(TextMapPropertyKey, value);
    }

    /// <inheritdoc cref="FrontMatterProperty"/>
    public string? FrontMatter => (string?)GetValue(FrontMatterProperty);

    /// <inheritdoc cref="SelectionStartProperty"/>
    public int SelectionStart => (int)GetValue(SelectionStartProperty);

    /// <inheritdoc cref="SelectionLengthProperty"/>
    public int SelectionLength => (int)GetValue(SelectionLengthProperty);

    /// <summary>The selected text for copying; computed on request.</summary>
    public string SelectedText => TextMap.GetText(Selection.Range);

    /// <summary>The selection written as Markdown with its own formatting; computed on request.</summary>
    public string SelectedMarkdown => MdMarkdownWriter.Write(_blocks, TextMap, Source, Selection.Range, Guard);

    /// <summary>Свои расширения, следом общие — в порядке опроса.</summary>
    internal IReadOnlyList<MdExtension> ActiveExtensions => _activeExtensions;

    /// <summary>Обёртка кода расширений, который вьюер зовёт в потоке интерфейса; сбой уходит в <see cref="RenderFailed"/>.</summary>
    internal ExtensionGuard Guard { get; }

    /// <summary>Текст показанного документа: из него собраны блоки и карта; текст, который ещё разбирается, сюда не попадает.</summary>
    internal string Source { get; private set; } = string.Empty;

    internal SelectionController Selection { get; }

    internal MdBlockPanel? BlockPanel { get; set; }

    internal MdLineGutter? Gutter { get; private set; }

    internal MdRowMarks? RowMarks { get; private set; }

    // BYDESIGN: кисти пометок — логические потомки вьюера: иначе смена темы до них не доходит.
    /// <inheritdoc/>
    protected override IEnumerator LogicalChildren => WithThemeBrushes(base.LogicalChildren, _themeBrushes.All);

    /// <inheritdoc cref="ImagePolicyProperty"/>
    public static MdImagePolicy GetImagePolicy(DependencyObject element)
        => (MdImagePolicy)element.GetValue(ImagePolicyProperty);

    /// <inheritdoc cref="ImagePolicyProperty"/>
    public static void SetImagePolicy(DependencyObject element, MdImagePolicy value)
        => element.SetValue(ImagePolicyProperty, value);

    /// <inheritdoc cref="SharedExtensionsProperty"/>
    public static MdExtensionCollection? GetSharedExtensions(DependencyObject element)
        => (MdExtensionCollection?)element.GetValue(SharedExtensionsProperty);

    /// <inheritdoc cref="SharedExtensionsProperty"/>
    public static void SetSharedExtensions(DependencyObject element, MdExtensionCollection? value)
        => element.SetValue(SharedExtensionsProperty, value);

    /// <inheritdoc cref="ShowCodeCopyButtonProperty"/>
    public static bool GetShowCodeCopyButton(DependencyObject element)
        => (bool)element.GetValue(ShowCodeCopyButtonProperty);

    /// <inheritdoc cref="ShowCodeCopyButtonProperty"/>
    public static void SetShowCodeCopyButton(DependencyObject element, bool value)
        => element.SetValue(ShowCodeCopyButtonProperty, value);

    /// <inheritdoc cref="ShowCodeLineNumbersProperty"/>
    public static bool GetShowCodeLineNumbers(DependencyObject element)
        => (bool)element.GetValue(ShowCodeLineNumbersProperty);

    /// <inheritdoc cref="ShowCodeLineNumbersProperty"/>
    public static void SetShowCodeLineNumbers(DependencyObject element, bool value)
        => element.SetValue(ShowCodeLineNumbersProperty, value);

    /// <summary>
    /// The position of a source line in the coordinates of the viewer; the fraction is a part of
    /// the line.
    /// </summary>
    public double? GetSourceLineOffset(double line) => _positions.OffsetOf(line);

    /// <summary>The source line at a position of the viewer; the fraction is a part of the line.</summary>
    public double? GetSourceLineAt(double offset) => _positions.LineAt(offset);

    /// <summary>Где в координатах вьюера стояли бы пустые места строк, не будь у вьюера мест; строки — по возрастанию.</summary>
    internal double?[] GetGapOffsets(IReadOnlyList<int> lines) => _positions.GapOffsetsOf(lines);

    /// <summary>
    /// The source line visible at the fraction <paramref name="viewportFraction"/> of the height of
    /// the scrolling that holds the viewer: 0 is the top edge, 1 the bottom one.
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
    /// Scrolls the scrolling that holds the viewer so that a source line stands at the fraction
    /// <paramref name="viewportFraction"/> of its height. The scrolling must be by pixels.
    /// </summary>
    public bool ScrollToSourceLine(double line, double viewportFraction = 0)
        => StartScroll(() => ScrollToSourceLineOnce(line, viewportFraction));

    /// <summary>Прокрутка, в которой стоит вьюер.</summary>
    ScrollViewer? IMdScrollSide.Scroller => Scrollers.Nearest(this);

    /// <summary>
    /// Scrolls the scrolling that holds the viewer so that the start of a piece of the text map
    /// stands at the fraction <paramref name="viewportFraction"/> of its height; the piece may lie
    /// in blocks not created yet.
    /// </summary>
    public bool ScrollToRange(MdTextRange range, double viewportFraction = 0)
        => StartScroll(() => ScrollToRangeOnce(range, viewportFraction));

    /// <summary>
    /// Scrolls to an anchor of the document — a heading or a footnote — the same way as
    /// <see cref="ScrollToRange"/>; no such anchor — <c>false</c>.
    /// </summary>
    public bool ScrollToAnchor(string id, double viewportFraction = 0)
        => (TextMap.TryGetAnchor(id, out var range) || TextMap.TryGetAnchor(Uri.UnescapeDataString(id), out range))
           && ScrollToRange(range, viewportFraction);

    /// <summary>
    /// The position in the text map for a point in the coordinates of the viewer; with
    /// <paramref name="snapToText"/> — at the nearest text, without it outside text — <c>null</c>.
    /// </summary>
    public int? GetPositionFromPoint(Point point, bool snapToText) => Selection.FindOffset(point, snapToText);

    /// <summary>
    /// Rectangles of a piece of the text map in the coordinates of the viewer; only created blocks
    /// have them.
    /// </summary>
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

    /// <summary>
    /// The bounds of a piece of the text map in the coordinates of the viewer;
    /// <see cref="Rect.Empty"/> — the piece is not on created blocks.
    /// </summary>
    public Rect GetRangeBounds(MdTextRange range) => GetRangeRects(range).Aggregate(Rect.Empty, Rect.Union);

    /// <summary>
    /// The geometry of the document right in the coordinates of an element: pieces of text, source
    /// lines, points.
    /// </summary>
    public MdCoordinates GetCoordinates(UIElement relativeTo) => new(this, relativeTo);

    /// <summary>
    /// Marks under a point of the viewer in drawing order: over text — marks of text under it and
    /// full-width rows of its line; in the margin — bars and icons of its line.
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

    /// <summary>
    /// Marks at a position of the text map in drawing order: pieces of text where it stands and
    /// rows of its source line.
    /// </summary>
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
    /// Builds the document anew from the same text: the extensions ask the application again —
    /// resolvers, link templates, handlers. The selection and widget states stay.
    /// </summary>
    public void Refresh()
    {
        _pipeline = null;
        Rebuild(discardRunning: true);
    }

    /// <summary>Selects a piece of the text map.</summary>
    public void Select(int start, int length) => Selection.Select(start, length);

    /// <summary>Selects the whole document.</summary>
    public void SelectAll() => Selection.Select(0, TextMap.Length);

    /// <summary>Removes the selection, leaving an empty one at its start.</summary>
    public void ClearSelection() => Selection.Select(Selection.Range.Start, 0);

    /// <summary>
    /// Puts the selection on the clipboard as plain text, Markdown and HTML; raises
    /// <see cref="DataObject.CopyingEvent"/> first.
    /// </summary>
    public void Copy()
    {
        if (CreateCopyData() is { } data)
        {
            MdClipboard.Put(data);
        }
    }

    /// <summary>
    /// Puts the markup of the selection on the clipboard as plain text, to be pasted anywhere; also
    /// through <see cref="DataObject.CopyingEvent"/>.
    /// </summary>
    public void CopyMarkdown()
    {
        if (CreateCopyData(markdownAsText: true) is { } data)
        {
            MdClipboard.Put(data);
        }
    }

    /// <inheritdoc/>
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
        var html = Guard.Call(null, () => MdClipboard.Html(markdown, GetPipeline()), null);
        var data = MdClipboard.Data(markdownAsText ? markdown : SelectedText, markdown, html);
        return MdClipboard.Approve(this, data) ? data : null;
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new MdDocumentAutomationPeer(this);

    /// <inheritdoc/>
    protected override DependencyObject GetContainerForItemOverride() => new MdBlockItem();

    /// <inheritdoc/>
    protected override bool IsItemItsOwnContainerOverride(object item) => item is MdBlockItem;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);

        if (e.LeftButton == MouseButtonState.Pressed && Selection.Drag(e.GetPosition(this)) && !IsMouseCaptured)
        {
            CaptureMouse();
        }
    }

    /// <summary>
    /// The viewer lost the mouse in the middle of dragging — a window switch, a popup: dragging and
    /// auto-scrolling end.
    /// </summary>
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

    /// <inheritdoc/>
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
        viewer._forgetQueued = true;
        viewer.Rebuild(discardRunning: true);
    }

    /// <summary>Сменилось то, что контейнеры берут при создании: провайдер стилей, загрузчик картинок, адрес документа.</summary>
    private static void OnContainersSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (MarkdownViewer)d;
        viewer._templatesChanged = true;
        viewer.Rebuild();
    }

    private static void OnImagePolicyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownViewer)
        {
            OnContainersSourceChanged(d, e);
        }
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
    {
        ((MarkdownViewer)d).ClearSelection();
        CommandManager.InvalidateRequerySuggested();
    }

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

    /// <summary>Подписка — ровно на слои <see cref="MarkLayers"/>, каждый по разу, при любом изменении списка.</summary>
    private void OnMarkLayersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var layer in _subscribedLayers)
        {
            layer.Changed -= OnMarkLayerChanged;
        }

        _subscribedLayers = MarkLayers.Distinct().ToList();
        foreach (var layer in _subscribedLayers)
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
        Rebuild(discardRunning: true);
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
            var before = builder.Extensions.ToList();
            if (!Guard.Call(extension, () => extension.SetupPipeline(builder)))
            {
                builder.Extensions.Clear();
                builder.Extensions.AddRange(before);
            }
        }

        return _pipeline = Guard.Call(null, builder.Build, null) ?? MarkdownPipeline ?? ModelBuilder.DefaultPipeline;
    }

    /// <summary>
    /// Разбирает текст заново: короткий — сразу, длинный — в фоне, по одному разбору за раз. Правки,
    /// пришедшие во время разбора, схлопываются в одну — последнюю. <paramref name="discardRunning"/> —
    /// идущий разбор устарел целиком: другие настройка разбора, расширения или документ.
    /// </summary>
    private void Rebuild(bool discardRunning = false)
    {
        if (_parsing is not null)
        {
            _rebuildQueued = true;
            _discardRunning |= discardRunning;
            return;
        }

        StartParse();
    }

    private void StartParse()
    {
        if (_forgetQueued)
        {
            _forgetQueued = false;
            _identity.Forget();
        }

        var markdown = Markdown ?? string.Empty;
        if (StyleProvider is null)
        {
            Apply(new([], null, MdTextMap.Build([], markdown), markdown));
            return;
        }

        var pipeline = GetPipeline();
        var extensions = ActiveExtensions;
        if (BackgroundParseThreshold <= 0 || markdown.Length < BackgroundParseThreshold)
        {
            ParseHere(markdown, pipeline, extensions);
            return;
        }

        var parsing = Task.Run(() => Parse(markdown, pipeline, extensions));
        _parsing = parsing;
        parsing.ContinueWith(_ => Dispatcher.BeginInvoke(() => FinishParse(parsing)), TaskScheduler.Default);
    }

    /// <summary>Разбирает и кладёт текст в потоке интерфейса.</summary>
    private void ParseHere(string markdown, MarkdownPipeline pipeline, IReadOnlyList<MdExtension> extensions)
    {
        var document = Parse(markdown, pipeline, extensions);
        _identity.Commit();
        Apply(document);
    }

    /// <summary>
    /// Фоновый разбор кончился: ложится, если не устарел целиком, а потом разбирается последний текст,
    /// если он пришёл. Разбор упал в фоне — тот же текст разбирается в потоке интерфейса; если там он
    /// не упал, в <see cref="RenderFailed"/> уходят сбои фонового.
    /// </summary>
    private void FinishParse(Task<ParsedDocument> parsing)
    {
        _parsing = null;
        var queued = _rebuildQueued;
        var discard = _discardRunning;
        _rebuildQueued = false;
        _discardRunning = false;

        IReadOnlyList<(Exception Exception, MdExtension? Extension)> failures = parsing.Exception?.GetBaseException() is { } error
            ? [(error, null)]
            : parsing.IsCompletedSuccessfully ? parsing.Result.Failures : [];
        var failed = !parsing.IsCompletedSuccessfully || failures.Count > 0;

        if (failed && !queued)
        {
            var document = Parse(Markdown ?? string.Empty, GetPipeline(), ActiveExtensions);
            _identity.Commit();
            Apply(document);
            if (document.Failures.Count == 0)
            {
                RaiseRenderFailed(failures, isBackground: true);
            }

            return;
        }

        if (!failed && !discard)
        {
            _identity.Commit();
            Apply(parsing.Result);
        }

        if (queued || discard)
        {
            StartParse();
        }
    }

    /// <summary>Разбирает текст; упавший код расширения отдаёт узел встроенному разбору, упавший Markdig — запасному документу из строк исходника.</summary>
    private ParsedDocument Parse(string markdown, MarkdownPipeline pipeline, IReadOnlyList<MdExtension> extensions)
    {
        var failures = new List<(Exception Exception, MdExtension? Extension)>();
        var owners = new Dictionary<object, MdExtension>(ReferenceEqualityComparer.Instance);
        var guard = new ExtensionGuard((exception, extension) => failures.Add((exception, extension)), owners.GetValueOrDefault);
        try
        {
            var blocks = ModelBuilder.Parse(markdown, pipeline, extensions, _identity, out var frontMatter, commit: false, guard, owners);
            return new(blocks, frontMatter, MdTextMap.Build(blocks, markdown, guard), markdown) { Failures = failures, Owners = owners };
        }
        catch (Exception exception)
        {
            failures.Add((exception, null));
            var blocks = ModelBuilder.SourceDocument(markdown);
            return new(blocks, null, MdTextMap.Build(blocks, markdown), markdown) { Failures = failures };
        }
    }

    private void RaiseRenderFailed(IReadOnlyList<(Exception Exception, MdExtension? Extension)> failures, bool isBackground)
    {
        foreach (var (exception, extension) in failures)
        {
            RenderFailed?.Invoke(this, new(exception, extension, isBackground));
        }
    }

    /// <summary>
    /// Разобранный текст ложится правками: блок с прежним идентификатором остаётся в своём контейнере.
    /// Сменились шаблоны — контейнеры создаются заново. Выделение находит своё место в новом тексте,
    /// как отслеживаемый кусок; его блок или текст исчез — выделение снимается. Сбои разбора уходят в
    /// <see cref="RenderFailed"/>, когда документ лёг.
    /// </summary>
    private void Apply(ParsedDocument document)
    {
        var old = TextMap;
        var selected = Selection is { Range.IsEmpty: false } selection
            ? (Range: TextMap.Track(selection.Range), Backward: selection.Ends.Active < selection.Ends.Anchor)
            : default;
        var following = TrackFollowingMarks();

        Source = document.Markdown;
        _owners = document.Owners;
        TextMap = document.TextMap;
        SetValue(FrontMatterPropertyKey, document.FrontMatter);
        _states.Retain(_identity.Contains);
        _blocks.Apply(document.Blocks);
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
        RaiseRenderFailed(document.Failures, isBackground: false);
    }

    /// <summary>Разобранный текст: блоки, front matter, текстовая карта, сбои разбора и какое расширение создало какую вьюмодель.</summary>
    private sealed record ParsedDocument(IReadOnlyList<BlockVm> Blocks, string? FrontMatter, MdTextMap TextMap, string Markdown)
    {
        public IReadOnlyList<(Exception Exception, MdExtension? Extension)> Failures { get; init; } = [];

        public IReadOnlyDictionary<object, MdExtension> Owners { get; init; } = new Dictionary<object, MdExtension>();
    }
}