namespace MarkdigWpfViewer;

using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Abstractions;
using Extensions;
using Markdig;
using Presenters;
using Scrolling;
using Selection;
using ViewModels;

/// <summary>Контрол, который показывает markdown-текст блоками по шаблонам темы.</summary>
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

    private static readonly DependencyPropertyKey SelectionStartPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(SelectionStart),
            typeof(int),
            typeof(MarkdownViewer),
            new(0));

    private static readonly DependencyPropertyKey SelectionLengthPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(SelectionLength),
            typeof(int),
            typeof(MarkdownViewer),
            new(0));

    /// <summary>Начало выделения в текстовой карте.</summary>
    public static readonly DependencyProperty SelectionStartProperty = SelectionStartPropertyKey.DependencyProperty;

    /// <summary>Длина выделения в текстовой карте.</summary>
    public static readonly DependencyProperty SelectionLengthProperty = SelectionLengthPropertyKey.DependencyProperty;

    private readonly SelectionController _selection;
    private readonly SourcePositions _positions;
    private MarkdownPipeline? _pipeline;
    private bool _viewportNotificationQueued;
    private (double Line, double Fraction, int Attempts)? _pendingScroll;

    static MarkdownViewer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MarkdownViewer), new FrameworkPropertyMetadata(typeof(MarkdownViewer)));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownViewer),
            new CommandBinding(
                ApplicationCommands.Copy,
                (sender, _) => ((MarkdownViewer)sender).Copy(),
                (sender, e) => e.CanExecute = ((MarkdownViewer)sender).SelectionLength > 0));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownViewer),
            new CommandBinding(ApplicationCommands.SelectAll, (sender, _) => ((MarkdownViewer)sender).SelectAll()));
    }

    public MarkdownViewer()
    {
        // BYDESIGN: своя коллекция через SetCurrentValue, а не локальным значением — так её
        // перекрывает общая из стиля, а XAML, который читает свойство мимо геттера, видит не null.
        SetCurrentValue(ExtensionsProperty, new MdExtensionCollection());

        _selection = new SelectionController(this);
        _selection.Changed += OnSelectionChanged;
        _positions = new SourcePositions(this);

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

    /// <summary>Текстовая карта показанного документа.</summary>
    public MdTextMap TextMap { get; private set; } = MdTextMap.Empty;

    public int SelectionStart => (int)GetValue(SelectionStartProperty);

    public int SelectionLength => (int)GetValue(SelectionLengthProperty);

    /// <summary>Выделенный текст для копирования; считается по запросу.</summary>
    public string SelectedText => TextMap.GetText(_selection.Range);

    internal IReadOnlyList<MdExtension> ActiveExtensions
        => GetValue(ExtensionsProperty) as MdExtensionCollection ?? (IReadOnlyList<MdExtension>)[];

    internal SelectionController Selection => _selection;

    internal MdBlockPanel? BlockPanel { get; set; }

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

        var point = scroller.TranslatePoint(new Point(0, viewportFraction * scroller.ViewportHeight), this);
        return GetSourceLineAt(point.Y);
    }

    /// <summary>
    /// Прокручивает прокрутку, в которой стоит вьюер, так, чтобы строка исходника встала в долю
    /// <paramref name="viewportFraction"/> её высоты. Прокрутка должна быть попиксельной.
    /// </summary>
    public bool ScrollToSourceLine(double line, double viewportFraction = 0)
    {
        if (!ScrollToSourceLineOnce(line, viewportFraction))
        {
            return false;
        }

        _pendingScroll = (line, viewportFraction, 3);
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, RefineScroll);
        return true;
    }

    internal void OnViewportMaybeChanged()
    {
        if (_viewportNotificationQueued || ViewportChanged is null)
        {
            return;
        }

        _viewportNotificationQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            _viewportNotificationQueued = false;
            ViewportChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <summary>Выделяет кусок текстовой карты.</summary>
    public void Select(int start, int length) => _selection.Select(start, length);

    public void SelectAll() => _selection.Select(0, TextMap.Length);

    public void ClearSelection() => _selection.Select(_selection.Range.Start, 0);

    protected override DependencyObject GetContainerForItemOverride() => new MdBlockItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is MdBlockItem;

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);

        if (e.ClickCount > 1 || SelectionController.IsInteractive(e.OriginalSource as DependencyObject, this))
        {
            return;
        }

        if (_selection.Press(e.GetPosition(this), Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)))
        {
            Focus();
        }
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);

        if (e.LeftButton == MouseButtonState.Pressed && _selection.Drag(e.GetPosition(this)) && !IsMouseCaptured)
        {
            CaptureMouse();
        }
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);

        if (_selection.Release())
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
        => ((MarkdownViewer)d).Rebuild();

    private static void OnDecorationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).UpdateLayer();

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
        var range = _selection.Range;
        SetValue(SelectionStartPropertyKey, range.Start);
        SetValue(SelectionLengthPropertyKey, range.Length);
        UpdateLayer();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Copy()
    {
        try
        {
            Clipboard.SetText(SelectedText);
        }
        catch (COMException)
        {
            // BYDESIGN: буфер обмена бывает занят другим приложением — копирование тогда просто
            // не удаётся, а вьюер работает дальше.
        }
    }

    private bool ScrollToSourceLineOnce(double line, double viewportFraction)
    {
        if (Scrollers.Nearest(this) is not { } scroller || GetSourceLineOffset(line) is not { } offset)
        {
            return false;
        }

        var position = TranslatePoint(new Point(0, offset), scroller).Y;
        var shift = position - viewportFraction * scroller.ViewportHeight;
        if (Math.Abs(shift) >= 0.5)
        {
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset + shift);
        }

        return true;
    }

    /// <summary>
    /// После прокрутки созданные блоки уточняют свои высоты, и строка съезжает — прокрутка
    /// повторяется, пока строка не встанет на место.
    /// </summary>
    private void RefineScroll()
    {
        if (_pendingScroll is not { } pending)
        {
            return;
        }

        _pendingScroll = null;
        if (pending.Attempts > 0 && ScrollToSourceLineOnce(pending.Line, pending.Fraction))
        {
            _pendingScroll = pending with { Attempts = pending.Attempts - 1 };
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, RefineScroll);
        }
    }

    private void UpdateLayer()
    {
        SetValue(MdSelection.LayerProperty, new MdDecorationLayer(TextMap, _selection.Range, SelectionBrush, Decorations ?? []));
    }

    private void ResetPipeline()
    {
        _pipeline = null;
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

    private void Rebuild()
    {
        var blocks = StyleProvider is null
            ? Array.Empty<BlockVm>()
            : ModelBuilder.Parse(Markdown ?? string.Empty, GetPipeline(), ActiveExtensions);

        TextMap = MdTextMap.Build(blocks);
        ItemsSource = blocks;
        _selection?.Reset();
    }
}
