namespace MarkdigWpfViewer;

using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Abstractions;
using Extensions;
using Identity;
using Markdig;
using Presenters;
using Scrolling;
using Selection;
using ViewModels;

/// <summary>Контрол, который показывает markdown-текст блоками по шаблонам темы.</summary>
[TemplatePart(Name = GutterPart, Type = typeof(MdLineGutter))]
public sealed class MarkdownViewer : ItemsControl
{
    /// <summary>Текст документа.</summary>
    public static readonly DependencyProperty MarkdownProperty =
        DependencyProperty.Register(
            nameof(Markdown),
            typeof(string),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.None, OnMarkdownChanged));

    /// <summary>Базовая настройка разбора Markdig; не задана — расширенная по умолчанию.</summary>
    public static readonly DependencyProperty MarkdownPipelineProperty =
        DependencyProperty.Register(
            nameof(MarkdownPipeline),
            typeof(MarkdownPipeline),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(null, OnPipelineChanged));

    /// <summary>Шаблоны встроенных блоков.</summary>
    public static readonly DependencyProperty StyleProviderProperty =
        DependencyProperty.Register(
            nameof(StyleProvider),
            typeof(IMdStyleProvider),
            typeof(MarkdownViewer),
            new(null, OnStyleProviderChanged));

    /// <summary>Расширения под свой синтаксис.</summary>
    public static readonly DependencyProperty ExtensionsProperty =
        DependencyProperty.Register(
            nameof(Extensions),
            typeof(MdExtensionCollection),
            typeof(MarkdownViewer),
            new(null, OnExtensionsChanged));

    /// <summary>Подсветки кусков текста под выделением: найденное, правки диффа.</summary>
    public static readonly DependencyProperty DecorationsProperty =
        DependencyProperty.Register(
            nameof(Decorations),
            typeof(IReadOnlyList<MdDecoration>),
            typeof(MarkdownViewer),
            new(null, OnDecorationsChanged));

    /// <summary>Кисть выделения; рисуется под текстом.</summary>
    public static readonly DependencyProperty SelectionBrushProperty =
        DependencyProperty.Register(
            nameof(SelectionBrush),
            typeof(Brush),
            typeof(MarkdownViewer),
            new(null, OnDecorationsChanged));

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

    private const string GutterPart = "PART_Gutter";

    private readonly SourcePositions _positions;
    private readonly WidgetStates _states;
    private readonly ModelIdentity _identity = new();
    private readonly ModelCollection<BlockVm> _blocks = [];
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
            new(ApplicationCommands.SelectAll, (sender, _) => ((MarkdownViewer)sender).SelectAll()));
    }

    public MarkdownViewer()
    {
        _states = new(() => ActiveExtensions);
        SetValue(MdWidget.StatesProperty, _states);

        // BYDESIGN: своя коллекция через SetCurrentValue, а не локальным значением — так её
        // перекрывает общая из стиля, а XAML, который читает свойство мимо геттера, видит не null.
        SetCurrentValue(ExtensionsProperty, new MdExtensionCollection());
        ItemsSource = _blocks;

        Selection = new(this);
        Selection.Changed += OnSelectionChanged;
        _positions = new(this);

        // BYDESIGN: фокус по щелчку нужен ради копирования; показывать при этом вьюер целиком
        // внешняя прокрутка не должна.
        AddHandler(RequestBringIntoViewEvent, new RequestBringIntoViewEventHandler((_, e) => e.Handled = ReferenceEquals(e.TargetObject, this)));
    }

    /// <summary>Выделение изменилось.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Видимая часть документа могла сместиться: прокрутка, уточнились высоты блоков.</summary>
    public event EventHandler? ViewportChanged;

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

    public IMdStyleProvider? StyleProvider
    {
        get => (IMdStyleProvider?)GetValue(StyleProviderProperty);
        set => SetValue(StyleProviderProperty, value);
    }

    /// <summary>Расширения вьюера; общую коллекцию на несколько вьюеров можно задать стилем.</summary>
    public MdExtensionCollection Extensions
    {
        get => (MdExtensionCollection)GetValue(ExtensionsProperty);
        set => SetValue(ExtensionsProperty, value);
    }

    public IReadOnlyList<MdDecoration>? Decorations
    {
        get => (IReadOnlyList<MdDecoration>?)GetValue(DecorationsProperty);
        set => SetValue(DecorationsProperty, value);
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

    public MdTextMap TextMap
    {
        get => (MdTextMap)GetValue(TextMapProperty);
        private set => SetValue(TextMapPropertyKey, value);
    }

    public int SelectionStart => (int)GetValue(SelectionStartProperty);

    public int SelectionLength => (int)GetValue(SelectionLengthProperty);

    /// <summary>Выделенный текст для копирования; считается по запросу.</summary>
    public string SelectedText => TextMap.GetText(Selection.Range);

    /// <summary>Выделенное, записанное markdown со своим оформлением; считается по запросу.</summary>
    public string SelectedMarkdown => MdMarkdownWriter.Write(_blocks, TextMap, Markdown ?? string.Empty, Selection.Range);

    internal IReadOnlyList<MdExtension> ActiveExtensions
        => GetValue(ExtensionsProperty) as MdExtensionCollection ?? (IReadOnlyList<MdExtension>)[];

    internal SelectionController Selection { get; }

    internal MdBlockPanel? BlockPanel { get; set; }

    internal MdLineGutter? Gutter { get; private set; }

    public static bool GetShowCodeLineNumbers(DependencyObject element)
        => (bool)element.GetValue(ShowCodeLineNumbersProperty);

    public static void SetShowCodeLineNumbers(DependencyObject element, bool value)
        => element.SetValue(ShowCodeLineNumbersProperty, value);

    /// <summary>Позиция строки исходника в координатах вьюера; дробная часть — доля строки.</summary>
    public double? GetSourceLineOffset(double line) => _positions.OffsetOf(line);

    /// <summary>Строка исходника в позиции вьюера; дробная часть — доля строки.</summary>
    public double? GetSourceLineAt(double offset) => _positions.LineAt(offset);

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

    /// <summary>Ближайший вьюер, внутри которого стоит элемент.</summary>
    internal static MarkdownViewer? Owning(DependencyObject element)
    {
        for (DependencyObject? node = element; node is not null; node = node is Visual or Visual3D
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

    /// <summary>Выделяет кусок текстовой карты.</summary>
    public void Select(int start, int length) => Selection.Select(start, length);

    public void SelectAll() => Selection.Select(0, TextMap.Length);

    public void ClearSelection() => Selection.Select(Selection.Range.Start, 0);

    /// <summary>Кладёт выделенное в буфер обмена голым текстом и разметкой; перед этим поднимает <see cref="DataObject.CopyingEvent"/>.</summary>
    public void Copy()
    {
        if (CreateCopyData() is not { } data)
        {
            return;
        }

        try
        {
            Clipboard.SetDataObject(data, true);
        }
        catch (COMException)
        {
            // BYDESIGN: буфер обмена бывает занят другим приложением — копирование тогда просто
            // не удаётся, а вьюер работает дальше.
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Gutter = GetTemplateChild(GutterPart) as MdLineGutter;
    }

    /// <summary>Созданные блоки встали на другие места или сменились.</summary>
    internal void OnBlocksArranged() => Gutter?.InvalidateVisual();

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
    internal DataObject? CreateCopyData()
    {
        if (SelectionLength == 0)
        {
            return null;
        }

        var data = new DataObject();
        data.SetText(SelectedText);
        data.SetData(MdDataFormats.Markdown, new MemoryStream(Encoding.UTF8.GetBytes(SelectedMarkdown)));

        var copying = new DataObjectCopyingEventArgs(data, false);
        RaiseEvent(copying);
        return copying.CommandCancelled ? null : data;
    }

    protected override DependencyObject GetContainerForItemOverride() => new MdBlockItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MdBlockItem;

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);

        if (e.ClickCount > 1 || SelectionController.IsInteractive(e.OriginalSource as DependencyObject, this))
        {
            return;
        }

        if (Selection.Press(e.GetPosition(this), Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)))
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

    private static void OnStyleProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (MarkdownViewer)d;
        viewer._templatesChanged = true;
        viewer.Rebuild();
    }

    private static void OnDecorationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).UpdateLayer();

    private static void OnIsSelectionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).ClearSelection();

    private static void OnExtensionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (MarkdownViewer)d;

        if (e.OldValue is MdExtensionCollection oldExtensions)
        {
            oldExtensions.CollectionChanged -= viewer.OnExtensionsCollectionChanged;
        }

        if (e.NewValue is MdExtensionCollection newExtensions)
        {
            newExtensions.CollectionChanged += viewer.OnExtensionsCollectionChanged;
        }

        viewer.ResetPipeline();
    }

    private void OnExtensionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ResetPipeline();

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
        SetValue(MdSelection.LayerProperty, new MdDecorationLayer(TextMap, Selection.Range, SelectionBrush, Decorations ?? []));
    }

    private void ResetPipeline()
    {
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
        if (extensions.Count == 0)
        {
            return _pipeline = MarkdownPipeline ?? ModelBuilder.DefaultPipeline;
        }

        var builder = new MarkdownPipelineBuilder();
        foreach (var markdigExtension in (MarkdownPipeline ?? ModelBuilder.DefaultPipeline).Extensions)
        {
            builder.Extensions.Add(markdigExtension);
        }

        foreach (var extension in extensions)
        {
            extension.SetupPipeline(builder);
        }

        return _pipeline = builder.Build();
    }

    /// <summary>
    /// Новый текст ложится правками: блок с прежним идентификатором остаётся в своём контейнере.
    /// Сменились шаблоны — контейнеры создаются заново. Выделение переходит вместе со своими
    /// блоками; блок исчез — выделение снимается.
    /// </summary>
    private void Rebuild()
    {
        var selected = Selection is { Range.IsEmpty: false } selection
            ? (Anchor: TextMap.Locate(selection.Ends.Anchor), Active: TextMap.Locate(selection.Ends.Active))
            : default;

        var blocks = StyleProvider is null
            ? Array.Empty<BlockVm>()
            : ModelBuilder.Parse(Markdown ?? string.Empty, GetPipeline(), ActiveExtensions, _identity);

        TextMap = MdTextMap.Build(blocks);
        _states.Retain(_identity.Contains);
        _blocks.Apply(blocks);

        if (_templatesChanged)
        {
            _templatesChanged = false;
            Items.Refresh();
        }

        if (selected is ({ } anchor, { } active) && TextMap.Find(anchor) is { } start && TextMap.Find(active) is { } end)
        {
            Selection!.Restore(start, end);
        }
        else
        {
            Selection?.Reset();
        }

        Gutter?.InvalidateMeasure();
        Gutter?.InvalidateVisual();
    }
}