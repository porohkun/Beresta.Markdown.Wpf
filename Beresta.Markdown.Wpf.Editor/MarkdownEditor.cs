namespace Beresta.Markdown.Wpf.Editor;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Behaviors;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Presenters;
using AvalonSelection = ICSharpCode.AvalonEdit.Editing.Selection;
using Scrolling;
using Selection;
using ViewModels;

/// <summary>
/// A Markdown editor: the viewer that also edits the source of its document. The source lives in
/// AvalonEdit and goes to <see cref="MarkdownViewer.Markdown"/> after every edit, the drawing follows a
/// moment after the last one; the blocks being edited show the source in place of their drawing.
/// </summary>
public sealed class MarkdownEditor : MarkdownViewer
{
    /// <summary>How the editor edits its document; <see cref="MdEditMode.View"/> — it only shows.</summary>
    public static readonly DependencyProperty EditModeProperty =
        DependencyProperty.Register(
            nameof(EditMode),
            typeof(MdEditMode),
            typeof(MarkdownEditor),
            new FrameworkPropertyMetadata(MdEditMode.View, OnEditModeChanged),
            value => value is MdEditMode mode && Enum.IsDefined(mode));

    /// <summary>How front matter is edited; by default — as text.</summary>
    public static readonly DependencyProperty FrontMatterEditingProperty =
        DependencyProperty.Register(
            nameof(FrontMatterEditing),
            typeof(MdFrontMatterEditing),
            typeof(MarkdownEditor),
            new FrameworkPropertyMetadata(MdFrontMatterEditing.Text, OnFrontMatterEditingChanged),
            value => value is MdFrontMatterEditing editing && Enum.IsDefined(editing));

    /// <summary>The start of the selection in the source; with nothing selected — the caret. Binds both ways by default.</summary>
    public static readonly DependencyProperty SourceSelectionStartProperty =
        DependencyProperty.Register(
            nameof(SourceSelectionStart),
            typeof(int),
            typeof(MarkdownEditor),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSourceSelectionChanged));

    /// <summary>The length of the selection in the source; 0 — nothing is selected. Binds both ways by default.</summary>
    public static readonly DependencyProperty SourceSelectionLengthProperty =
        DependencyProperty.Register(
            nameof(SourceSelectionLength),
            typeof(int),
            typeof(MarkdownEditor),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSourceSelectionChanged));

    /// <summary>
    /// A new <see cref="MarkdownViewer.Markdown"/> from outside lands as one edit of the part that differs: one
    /// step of undo, the caret and the selection stay on their text. By default it replaces the source.
    /// </summary>
    public static readonly DependencyProperty MergeExternalTextProperty =
        DependencyProperty.Register(
            nameof(MergeExternalText),
            typeof(bool),
            typeof(MarkdownEditor),
            new FrameworkPropertyMetadata(false));

    private static readonly DependencyPropertyKey CanUndoPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(CanUndo),
            typeof(bool),
            typeof(MarkdownEditor),
            new(false));

    private static readonly DependencyPropertyKey CanRedoPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(CanRedo),
            typeof(bool),
            typeof(MarkdownEditor),
            new(false));

    /// <summary>There is an edit of the source to undo.</summary>
    public static readonly DependencyProperty CanUndoProperty = CanUndoPropertyKey.DependencyProperty;

    /// <summary>There is an undone edit of the source to redo.</summary>
    public static readonly DependencyProperty CanRedoProperty = CanRedoPropertyKey.DependencyProperty;

    /// <summary>Дальше этого страница за мышью за один шаг не прокручивается.</summary>
    private const double AutoScrollStep = 80;

    /// <summary>Свойства, через которые редактор следит за ключами темы, — по одному на ключ.</summary>
    private static readonly ConcurrentDictionary<string, DependencyProperty> Watchers = new();

    private readonly TextEditor _editor = new();
    private readonly SourceVersions _versions = new();
    private readonly TextLines _shownLines = new();
    private readonly SourceWindow _window;
    private readonly DispatcherTimer _commit;
    private readonly DispatcherTimer _autoScroll;
    private readonly Dictionary<string, LookStyle> _styles = [];
    private SourceLookIndex? _look;
    private Dictionary<int, (double Above, double Below)>? _fits;
    private readonly HashSet<string> _watched = [];
    private bool _watching;
    private bool _themeQueued;
    private List<(int FirstLine, int LastLine)> _windowContainers = [];
    private readonly Dictionary<string, List<double>> _cellWidths = [];
    private Dictionary<int, IReadOnlyList<(int Offset, double Before, double After)>>? _separators;
    private TextDocument _document;
    private (string Text, ITextSourceVersion Version)? _given;
    private bool _dirty;
    private long _dirtySince;
    private bool _taking;
    private bool _quiet;
    private bool _syncingSelection;
    private bool _selectionHeld;
    private bool _selectionPending;
    private (int Start, int Length) _selection;
    private bool _stepping;
    private bool _typed;
    private bool _caretMoved;
    private bool _checkEdges;
    private bool _windowWanted;
    private bool _reconcileQueued;
    private bool _refitQueued;
    private bool _dragging;
    private int? _clicked;
    private int? _linesBeforeDelete;

    static MarkdownEditor()
    {
        CodeHighlighterProperty.OverrideMetadata(
            typeof(MarkdownEditor),
            new FrameworkPropertyMetadata(MdAvalonCodeHighlighter.Instance, FrameworkPropertyMetadataOptions.Inherits));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownEditor),
            new(
                ApplicationCommands.Undo,
                (sender, _) => ((MarkdownEditor)sender).Undo(),
                (sender, e) => e.CanExecute = ((MarkdownEditor)sender).CanUndo));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownEditor),
            new(
                ApplicationCommands.Redo,
                (sender, _) => ((MarkdownEditor)sender).Redo(),
                (sender, e) => e.CanExecute = ((MarkdownEditor)sender).CanRedo));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownEditor),
            new(
                EditingCommands.ToggleBold,
                (sender, _) => EditingKeys.Emphasis(((MarkdownEditor)sender)._editor.TextArea, "**"),
                (sender, e) => e.CanExecute = ((MarkdownEditor)sender).EditsText));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownEditor),
            new(
                EditingCommands.ToggleItalic,
                (sender, _) => EditingKeys.Emphasis(((MarkdownEditor)sender)._editor.TextArea, "*"),
                (sender, e) => e.CanExecute = ((MarkdownEditor)sender).EditsText));

        CommandManager.RegisterClassCommandBinding(
            typeof(MarkdownEditor),
            new(
                PasteFormattedCommand,
                (sender, _) => ((MarkdownEditor)sender).PasteFormatted(ClipboardData()),
                (sender, e) => e.CanExecute = ((MarkdownEditor)sender).EditsText));
    }

    /// <summary>Creates an empty editor that only shows.</summary>
    public MarkdownEditor()
    {
        _commit = new(DispatcherPriority.Background, Dispatcher);
        _commit.Tick += (_, _) => CommitText();
        _autoScroll = new(DispatcherPriority.Input, Dispatcher) { Interval = TimeSpan.FromMilliseconds(40) };
        _autoScroll.Tick += (_, _) => AutoScroll();

        SetUpTextEditor();
        _window = new(_editor, ToShownLine, ToDocumentLine, MarginsOf);
        _window.Element.SetResourceReference(Panel.BackgroundProperty, "Md.Source.Background");

        _document = _editor.Document;
        Attach(_document);
        _editor.DocumentChanged += (_, _) => Reattach();

        var area = _editor.TextArea;
        area.TextView.ElementGenerators.Insert(0, new LineStruts(Strut));
        area.TextView.ElementGenerators.Insert(1, new LineMarkers(MarkerOf));
        area.TextView.ElementGenerators.Insert(2, new TableColumns(SeparatorsOf));
        area.TextView.SizeChanged += (_, e) =>
        {
            if (e.WidthChanged && _separators is not null)
            {
                _separators = null;
                area.TextView.Redraw();
            }
        };
        area.TextView.LineTransformers.Insert(0, new SourceColorizer(PiecesOn, () => area.Selection.Segments));
        area.TextView.BackgroundRenderers.Add(new SourceBackground(LooksAt));
        CommandManager.AddPreviewExecutedHandler(area, OnTextAreaCommand);
        area.AddHandler(CommandManager.ExecutedEvent, new ExecutedRoutedEventHandler(OnTextAreaCommandDone), handledEventsToo: true);
        area.Caret.PositionChanged += (_, _) => OnCaretMoved();
        area.SelectionChanged += (_, _) => OnCaretMoved();
        area.TextView.ScrollOffsetChanged += (_, _) => OnSelfScroll();
        area.TextView.VisualLinesChanged += (_, _) => QueueRefit();
        area.PreviewMouseMove += OnTextAreaMouseMove;
        area.PreviewMouseLeftButtonUp += (_, _) => EndDrag();
        area.PreviewKeyDown += OnTextAreaKeyDown;
        area.TextEntering += (_, e) =>
        {
            if (!e.Handled && e.Text is "\n" or "\r" or "\r\n" && EditingKeys.Enter(area))
            {
                e.Handled = true;
            }
        };
        area.TextEntered += (_, e) => EditingKeys.CloseFence(area, e.Text);
        DataObject.AddPastingHandler(area, EditingKeys.OnPasting);
        area.SetResourceReference(ContextMenuProperty, "Md.Source.ContextMenu");
        ViewportChanged += (_, _) => Place();
    }

    /// <summary>The source changed: typing, an undo, a control, a new text.</summary>
    public event EventHandler<MdSourceChangedEventArgs>? SourceChanged;

    /// <summary>The caret or the selection in the source moved.</summary>
    public event EventHandler? SourceSelectionChanged;

    /// <summary>Новый разбор лёг на документ: его текстовая карта — для показанного текста.</summary>
    internal event Action? DocumentApplied;

    /// <summary>
    /// Ctrl+Shift+V: pastes the HTML from the clipboard as Markdown in place of the selection; without HTML —
    /// the text, as an ordinary paste.
    /// </summary>
    public static RoutedUICommand PasteFormattedCommand { get; } =
        new("Paste with formatting", nameof(PasteFormattedCommand), typeof(MarkdownEditor), [new KeyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift)]);

    /// <inheritdoc cref="EditModeProperty"/>
    public MdEditMode EditMode
    {
        get => (MdEditMode)GetValue(EditModeProperty);
        set => SetValue(EditModeProperty, value);
    }

    /// <inheritdoc cref="FrontMatterEditingProperty"/>
    public MdFrontMatterEditing FrontMatterEditing
    {
        get => (MdFrontMatterEditing)GetValue(FrontMatterEditingProperty);
        set => SetValue(FrontMatterEditingProperty, value);
    }

    /// <inheritdoc cref="MergeExternalTextProperty"/>
    public bool MergeExternalText
    {
        get => (bool)GetValue(MergeExternalTextProperty);
        set => SetValue(MergeExternalTextProperty, value);
    }

    /// <inheritdoc cref="CanUndoProperty"/>
    public bool CanUndo => (bool)GetValue(CanUndoProperty);

    /// <inheritdoc cref="CanRedoProperty"/>
    public bool CanRedo => (bool)GetValue(CanRedoProperty);

    /// <summary>
    /// The caret in the source: the position of the character it stands before. In
    /// <see cref="MdEditMode.Blocks"/> its block becomes edited.
    /// </summary>
    public int SourceCaretOffset
    {
        get => _editor.CaretOffset;
        set
        {
            WantWindow();
            _editor.CaretOffset = value;
        }
    }

    /// <inheritdoc cref="SourceSelectionStartProperty"/>
    public int SourceSelectionStart
    {
        get => (int)GetValue(SourceSelectionStartProperty);
        set => SetValue(SourceSelectionStartProperty, value);
    }

    /// <inheritdoc cref="SourceSelectionLengthProperty"/>
    public int SourceSelectionLength
    {
        get => (int)GetValue(SourceSelectionLengthProperty);
        set => SetValue(SourceSelectionLengthProperty, value);
    }

    /// <summary>The AvalonEdit that holds the source: its document, options and look.</summary>
    public TextEditor TextEditor => _editor;

    /// <summary>Сколько отрисовка ждёт после последней правки, прежде чем разобрать текст заново.</summary>
    internal TimeSpan CommitDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Дольше этого отрисовка не ждёт, даже если правки идут без перерыва.</summary>
    internal TimeSpan CommitLimit { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Строки окна в документе, с единицы; окна нет — <c>null</c>.</summary>
    internal (int First, int Last)? WindowLines => _window.IsOpen ? _window.Lines : null;

    /// <summary>Сколько строк разложил AvalonEdit; разложенных нет — -1.</summary>
    internal int LaidOutLines => _editor.TextArea.TextView.VisualLinesValid ? _editor.TextArea.TextView.VisualLines.Count : -1;

    /// <summary>Холст окна — элемент, который стоит во вьюере вместо правимых блоков.</summary>
    internal FrameworkElement WindowElement => _window.Element;

    private ScrollViewer? Scroller => ((IMdScrollSide)this).Scroller;

    /// <summary>Selects a piece of the source; in <see cref="MdEditMode.Blocks"/> its blocks become edited.</summary>
    public void SelectSource(int start, int length)
    {
        WantWindow();

        // BYDESIGN: AvalonEdit ставит выделение раньше курсора — свойства и событие получают только итог
        _selectionHeld = true;
        try
        {
            _editor.Select(start, length);
        }
        finally
        {
            _selectionHeld = false;
        }

        SyncSelection();
    }

    /// <summary>Puts text in place of the selection in the source, or at the caret.</summary>
    public void InsertText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        WantWindow();
        _editor.TextArea.Selection.ReplaceSelectionWithText(text);
    }

    /// <summary>
    /// Replaces <paramref name="length"/> characters of the source from <paramref name="start"/> with text in one step
    /// of undo; the caret and the selection stay on their text, and the page does not follow the caret.
    /// </summary>
    public void ReplaceSource(int start, int length, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (start < 0 || start > _document.TextLength)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (length < 0 || start + length > _document.TextLength)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        _quiet = true;
        try
        {
            _document.Replace(start, length, text);
        }
        finally
        {
            _quiet = false;
        }
    }

    /// <summary>Draws the edited source at once, without waiting for the pause after the last edit.</summary>
    public void CommitText()
    {
        _commit.Stop();
        if (!_dirty)
        {
            return;
        }

        _dirty = false;
        ParseMarkdown();
    }

    /// <summary>Undoes the last edit of the source; the caret goes to its place.</summary>
    public void Undo() => Step(redo: false);

    /// <summary>Redoes the last undone edit of the source; the caret goes to its place.</summary>
    public void Redo() => Step(redo: true);

    /// <summary>Строки вне окна, которые AvalonEdit показывает в своей видимой части.</summary>
    internal IReadOnlyList<int> ForeignLines() => _window.ForeignLines();

    /// <summary>
    /// Редактор потерял клавиатурный фокус — набор кончился: отрисовка догоняет текст сразу, а в блочном
    /// режиме окно закрывается, если фокус не вернулся.
    /// </summary>
    internal void LoseFocus()
    {
        CommitText();

        // BYDESIGN: решение — после раскладки: пересозданный контейнер окна возвращает AvalonEdit с фокусом
        if (EditMode == MdEditMode.Blocks)
        {
            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                () =>
                {
                    if (!IsKeyboardFocusWithin && EditMode == MdEditMode.Blocks)
                    {
                        CloseWindow();
                    }
                });
        }
    }

    /// <summary>Курсор виден в прокрутке, где стоит редактор.</summary>
    internal bool IsCaretVisible()
    {
        if (!_window.IsOpen || Scroller is not { } scroller || !_window.Element.IsDescendantOf(scroller))
        {
            return false;
        }

        var (top, height) = _window.Caret();
        var y = _window.Element.TranslatePoint(new(0, top), scroller).Y;
        return y + height > 0 && y < scroller.ViewportHeight;
    }

    /// <summary>
    /// Щелчок по нарисованному: курсор на знак исходника под точкой вьюера, а вне текста — в начало
    /// строки исходника под ней; её блок становится правимым. Под точкой нет строки — <c>false</c>.
    /// </summary>
    internal bool PlaceCaretAt(Point point)
    {
        if (GetSourceLineAt(point.Y) is not { } line)
        {
            return false;
        }

        var shownLine = (int)Math.Floor(line);
        var target = ToDocumentLine(shownLine);
        if (FrontMatterEditing != MdFrontMatterEditing.Text && target <= FrontMatterEnd())
        {
            return false;
        }

        _windowWanted = true;
        _editor.TextArea.ClearSelection();
        _editor.CaretOffset = OffsetAt(point, shownLine) ?? _document.GetLineByNumber(target).Offset;
        _clicked = _editor.CaretOffset;
        Reconcile();
        return true;
    }

    /// <summary>
    /// Двойной щелчок по нарисованному: первый открыл блок, и в исходнике выделяется слово у места
    /// первого щелчка — так, как его выделил бы AvalonEdit. Первый щелчок был не по нарисованному —
    /// <c>false</c>.
    /// </summary>
    internal bool SelectClickedWord()
    {
        if (_clicked is not { } offset || offset > _document.TextLength)
        {
            return false;
        }

        _clicked = null;
        var line = _document.GetLineByOffset(offset);
        var start = TextUtilities.GetNextCaretPosition(_document, Math.Min(offset + 1, line.EndOffset), LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol);
        start = Math.Max(start, line.Offset);
        var end = TextUtilities.GetNextCaretPosition(_document, start, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol);
        end = end < 0 ? line.EndOffset : Math.Min(end, line.EndOffset);

        var area = _editor.TextArea;
        area.Caret.Offset = end;
        area.Selection = AvalonSelection.Create(area, start, end);
        return true;
    }

    /// <summary>Протяжка: конец выделения — место исходника под точкой вьюера, где бы она ни была.</summary>
    internal void ExtendSelectionTo(Point point)
    {
        if (PositionAt(new(point.X, Math.Clamp(point.Y, 0, Math.Max(0, ActualHeight - 1)))) is not { } offset)
        {
            return;
        }

        var area = _editor.TextArea;
        var old = area.Caret.Position;
        area.Caret.Offset = offset;
        area.Selection = area.Selection.StartSelectionOrSetEndpoint(old, area.Caret.Position);
    }

    /// <inheritdoc/>
    protected override void OnMarkdownChanged()
    {
        // BYDESIGN: свой текст приходит на каждую правку — разбирает его отрисовка по таймеру
        if (!Take(Markdown ?? string.Empty))
        {
            base.OnMarkdownChanged();
        }
    }

    /// <inheritdoc/>
    protected override void OnDocumentApplied()
    {
        DocumentApplied?.Invoke();
        _look = null;
        _fits = null;
        _styles.Clear();
        _separators = null;
        _cellWidths.Clear();
        _editor.TextArea.TextView.Redraw();
        if (_window.IsOpen)
        {
            ShowWindow();
            _checkEdges = true;
            QueueReconcile();
        }
    }

    /// <inheritdoc/>
    protected override void ApplySourceEdit(MdSourceEdit edit)
    {
        var shown = ShownMarkdown;
        var start = _versions.Map(shown, edit.Start, _document, AnchorMovementType.AfterInsertion);
        var end = edit.Length == 0 ? start : _versions.Map(shown, edit.Start + edit.Length, _document, AnchorMovementType.BeforeInsertion);
        if (start is not { } from || end is not { } to || to < from)
        {
            return;
        }

        _document.Replace(from, to - from, edit.Text);
        CommitText();
    }

    /// <inheritdoc/>
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && SelectClickedWord())
        {
            e.Handled = true;
            return;
        }

        _clicked = null;
        var source = e.OriginalSource as DependencyObject;
        if (EditMode == MdEditMode.Blocks
            && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control)
            && !IsInWindow(source)
            && !IsControl(source)
            && PlaceCaretAt(e.GetPosition(this)))
        {
            _editor.TextArea.Focus();
            _dragging = CaptureMouse();
            e.Handled = true;
            return;
        }

        // Щелчок по документу, который правится, переносит фокус в редактор
        if (EditMode != MdEditMode.View && !IsKeyboardFocusWithin)
        {
            Focus();
        }

        base.OnPreviewMouseLeftButtonDown(e);
    }

    /// <inheritdoc/>
    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        if (_dragging)
        {
            ExtendSelectionTo(e.GetPosition(this));
            FollowMouse();
            e.Handled = true;
            return;
        }

        base.OnPreviewMouseMove(e);
    }

    /// <inheritdoc/>
    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_dragging)
        {
            EndDrag();
            e.Handled = true;
            return;
        }

        base.OnPreviewMouseLeftButtonUp(e);
    }

    /// <inheritdoc/>
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (ReferenceEquals(e.OriginalSource, this) && _dragging)
        {
            _dragging = false;
            _autoScroll.Stop();
        }
    }

    /// <inheritdoc/>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (OpensWindow(e.OriginalSource)
            && Keyboard.Modifiers == ModifierKeys.None
            && e.Key is Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown or Key.Enter)
        {
            OpenAtCaret();
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    /// <inheritdoc/>
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        if (OpensWindow(e.OriginalSource) && e.Text.Length > 0)
        {
            OpenAtCaret();
            _editor.TextArea.PerformTextInput(e.Text);
            e.Handled = true;
            return;
        }

        base.OnPreviewTextInput(e);
    }

    /// <inheritdoc/>
    protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnIsKeyboardFocusWithinChanged(e);
        if (e.NewValue is false)
        {
            LoseFocus();
        }
    }

    /// <inheritdoc/>
    protected override void OnQueryCursor(QueryCursorEventArgs e)
    {
        base.OnQueryCursor(e);
        if (!e.Handled && EditMode == MdEditMode.Blocks && e.OriginalSource is TextElement or MdTextBlock)
        {
            e.Cursor = Cursors.IBeam;
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        QueueThemeChange();
    }

    private static void OnEditModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownEditor)d).SwitchMode((MdEditMode)e.OldValue, (MdEditMode)e.NewValue);

    private static void OnFrontMatterEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MarkdownEditor)d;
        editor.UpdateFrontMatter();
        editor.QueueReconcile();
    }

    /// <summary>Выделение задали снаружи: оно ложится в AvalonEdit, прижатое к длине исходника.</summary>
    private static void OnSourceSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MarkdownEditor)d;
        if (editor._syncingSelection)
        {
            return;
        }

        var length = editor._document.TextLength;
        var start = Math.Clamp(editor.SourceSelectionStart, 0, length);
        editor.SelectSource(start, Math.Clamp(editor.SourceSelectionLength, 0, length - start));
    }

    /// <summary>Номер блока верхнего уровня, задевающего строку, а в промежутке между блоками — следующего.</summary>
    private static int BlockAt(IReadOnlyList<MdSourceSpan> spans, int line)
    {
        var low = 0;
        var high = spans.Count;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (spans[middle].EndLine >= line)
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        return Math.Min(low, spans.Count - 1);
    }

    private static (int First, int Last) Union((int First, int Last) lines, (int First, int Last) other)
        => (Math.Min(lines.First, other.First), Math.Max(lines.Last, other.Last));

    private static IEnumerable<DependencyObject> Ancestors(DependencyObject? node)
    {
        for (; node is not null; node = node is Visual ? VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node))
        {
            yield return node;
        }
    }

    private void SetUpTextEditor()
    {
        _editor.WordWrap = true;
        _editor.ShowLineNumbers = false;
        _editor.Padding = new(0);
        _editor.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        _editor.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _editor.Options.EnableHyperlinks = false;
        _editor.Options.EnableEmailHyperlinks = false;
        _editor.SetBinding(FontFamilyProperty, new Binding(nameof(FontFamily)) { Source = this });
        _editor.SetResourceReference(FontSizeProperty, "Md.Paragraph.FontSize");
        _editor.SetBinding(ForegroundProperty, new Binding(nameof(Foreground)) { Source = this });
        _editor.SetResourceReference(BackgroundProperty, "Md.Source.Background");
        _editor.Loaded += (_, _) =>
        {
            if (_editor.Template?.FindName("PART_ScrollViewer", _editor) is ScrollViewer inner)
            {
                NestedScroll.SetPassWheelToParent(inner, true);
            }
        };
    }

    /// <summary>Смена режима: контролы, выделение вьюера и окно — по режиму; сверху остаётся та же строка исходника.</summary>
    private void SwitchMode(MdEditMode old, MdEditMode mode)
    {
        var top = GetVisibleSourceLine();
        SetCurrentValue(AreControlsEditableProperty, mode != MdEditMode.View);
        SetCurrentValue(IsSelectionEnabledProperty, mode is MdEditMode.View or MdEditMode.Controls);
        UpdateFrontMatter();

        // Из полного режима в блочный окно остаётся на блоках курсора, только если в нём печатали
        _windowWanted = mode == MdEditMode.Blocks && _editor.TextArea.IsKeyboardFocusWithin;
        if (mode is MdEditMode.View or MdEditMode.Controls || (mode == MdEditMode.Blocks && !_windowWanted))
        {
            CloseWindow();
        }
        else
        {
            QueueReconcile();
        }

        if (mode == MdEditMode.View)
        {
            CommitText();
        }

        if (top is { } line && (old is MdEditMode.Blocks or MdEditMode.Full || mode is MdEditMode.Blocks or MdEditMode.Full))
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => ScrollToSourceLine(line));
        }
    }

    /// <summary>
    /// Новое значение <see cref="MarkdownViewer.Markdown"/>: свой текст ничего не делает — <c>true</c>; чужой с
    /// <see cref="MergeExternalText"/> ложится одной правкой того, что отличается, а без него — так, как его
    /// принимает AvalonEdit, и закрывает окно.
    /// </summary>
    private bool Take(string text)
    {
        if (_given is { } given && string.Equals(text, given.Text, StringComparison.Ordinal))
        {
            _versions.Remember(text, given.Version);
            return true;
        }

        if (!string.Equals(text, _document.Text, StringComparison.Ordinal))
        {
            _taking = true;
            _quiet = MergeExternalText;
            try
            {
                if (MergeExternalText)
                {
                    Merge(text);
                }
                else
                {
                    _editor.Text = text;
                }
            }
            finally
            {
                _taking = false;
                _quiet = false;
            }

            if (_window.IsOpen && !MergeExternalText)
            {
                _window.Close();
                ClearSourceWindow();
                QueueReconcile();
            }
        }

        _dirty = false;
        _commit.Stop();
        _given = (text, _document.Version);
        _versions.Remember(text, _document.Version);
        return false;
    }

    /// <summary>Текст документа уходит в <see cref="MarkdownViewer.Markdown"/>; разбор его ждёт отрисовки.</summary>
    private void Push()
    {
        var text = _document.Text;
        _given = (text, _document.Version);
        _versions.Remember(text, _document.Version);
        SetCurrentValue(MarkdownProperty, text);
    }

    /// <summary>Текст ложится в документ одной правкой между общими с ним началом и концом; суррогатные пары не режутся.</summary>
    private void Merge(string text)
    {
        var current = _document.Text;
        var limit = Math.Min(current.Length, text.Length);
        var prefix = 0;
        while (prefix < limit && current[prefix] == text[prefix])
        {
            prefix++;
        }

        if (prefix > 0 && char.IsHighSurrogate(text[prefix - 1]))
        {
            prefix--;
        }

        var suffix = 0;
        while (suffix < limit - prefix && current[^(suffix + 1)] == text[^(suffix + 1)])
        {
            suffix++;
        }

        if (suffix > 0 && char.IsLowSurrogate(text[^suffix]))
        {
            suffix--;
        }

        _document.Replace(prefix, current.Length - prefix - suffix, text.Substring(prefix, text.Length - prefix - suffix));
    }

    /// <summary>Отмена или возврат правки: курсор встаёт туда, где документ поменялся, окно — на его блоки, а отрисовка догоняет её сразу.</summary>
    private void Step(bool redo)
    {
        var stack = _document.UndoStack;
        if (redo ? !stack.CanRedo : !stack.CanUndo)
        {
            return;
        }

        int? at = null;
        void Track(object? sender, DocumentChangeEventArgs e) => at = e.Offset + e.InsertionLength;

        _stepping = true;
        _document.Changed += Track;
        try
        {
            if (redo)
            {
                stack.Redo();
            }
            else
            {
                stack.Undo();
            }
        }
        finally
        {
            _document.Changed -= Track;
            _stepping = false;
        }

        if (at is { } offset)
        {
            WantWindow();
            _editor.TextArea.ClearSelection();
            _editor.CaretOffset = Math.Min(offset, _document.TextLength);
        }

        CommitText();
    }

    /// <summary>Правка документа: отрисовка догонит её через секунду после последней правки и не позже чем через пять после первой.</summary>
    private void OnDocumentChanged(object? sender, DocumentChangeEventArgs e)
    {
        if (!_taking)
        {
            var now = Environment.TickCount64;
            if (!_dirty)
            {
                _dirty = true;
                _dirtySince = now;
            }

            var due = Math.Min(now + (long)CommitDelay.TotalMilliseconds, _dirtySince + (long)CommitLimit.TotalMilliseconds);
            _commit.Stop();
            _commit.Interval = TimeSpan.FromMilliseconds(Math.Max(0, due - now));
            _commit.Start();
            _typed |= !_stepping && !_quiet;
            QueueReconcile();
        }

        if (_separators is { Count: > 0 } separators && separators.ContainsKey(_document.GetLineByOffset(Math.Min(e.Offset, _document.TextLength)).LineNumber))
        {
            _separators = null;
            _editor.TextArea.TextView.Redraw();
        }
        else if (_separators is not null)
        {
            _separators = null;
        }

        SourceChanged?.Invoke(this, new(new MdSourceEdit(e.Offset, e.RemovalLength, e.InsertedText.Text)));
    }

    /// <summary>
    /// Курсор или выделение сдвинулись: окно встанет на их блоки, а свойства выделения догонят AvalonEdit, когда
    /// кончится правка документа. Сдвиг от правки, которая не набор, страницу к курсору не ведёт.
    /// </summary>
    private void OnCaretMoved()
    {
        _caretMoved |= !_quiet;
        QueueReconcile();
        if (_document.IsInUpdate || _selectionHeld)
        {
            _selectionPending = true;
        }
        else
        {
            SyncSelection();
        }
    }

    /// <summary>Правка документа кончилась: текст уходит в <see cref="MarkdownViewer.Markdown"/>, свойства выделения догоняют AvalonEdit.</summary>
    private void OnUpdateFinished(object? sender, EventArgs e)
    {
        if (!_taking)
        {
            Push();
        }

        if (_selectionPending)
        {
            SyncSelection();
        }
    }

    /// <summary>Свойства выделения — по AvalonEdit; начало или длина сдвинулись — событие.</summary>
    private void SyncSelection()
    {
        _selectionPending = false;
        var current = (Start: _editor.SelectionStart, Length: _editor.SelectionLength);
        _syncingSelection = true;
        try
        {
            SetCurrentValue(SourceSelectionStartProperty, current.Start);
            SetCurrentValue(SourceSelectionLengthProperty, current.Length);
        }
        finally
        {
            _syncingSelection = false;
        }

        if (current != _selection)
        {
            _selection = current;
            SourceSelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>AvalonEdit прокрутился сам, вслед за курсором: пересчёт окна вернёт прокрутку до отрисовки.</summary>
    private void OnSelfScroll()
    {
        if (!_window.IsPlacing)
        {
            QueueReconcile();
        }
    }

    private void OnUndoStackChanged(object? sender, PropertyChangedEventArgs e) => UpdateUndo();

    private void UpdateUndo()
    {
        SetValue(CanUndoPropertyKey, _document.UndoStack.CanUndo);
        SetValue(CanRedoPropertyKey, _document.UndoStack.CanRedo);
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>Исходник правится текстом: весь — в полном режиме, блоки под курсором — в блочном, когда окно открыто.</summary>
    private bool EditsText => EditMode == MdEditMode.Full || (EditMode == MdEditMode.Blocks && _window.IsOpen);

    /// <summary>
    /// Кусок карты на месте куска нынешнего документа — через текст, который показывает вьюер; кусок не
    /// нарисован или ещё не показан — <c>null</c>.
    /// </summary>
    internal MdTextRange? MapRange(int start, int end)
    {
        var shown = ShownMarkdown;
        var from = TextMap.PositionOfSource(_versions.ToText(shown, start, _document, AnchorMovementType.AfterInsertion));
        var to = TextMap.PositionOfSource(_versions.ToText(shown, end, _document, AnchorMovementType.BeforeInsertion));
        return from is { } a && to is { } b && b > a ? new MdTextRange(a, b - a) : null;
    }

    /// <summary>Данные буфера обмена; буфер занят другой программой — <c>null</c>.</summary>
    private static IDataObject? ClipboardData()
    {
        try
        {
            return Clipboard.GetDataObject();
        }
        catch (ExternalException)
        {
            return null;
        }
    }

    /// <summary>Вставка с форматированием из данных буфера обмена; данных нет — ничего.</summary>
    internal void PasteFormatted(IDataObject? data)
    {
        if (data is not null)
        {
            EditingKeys.PasteFormatted(_editor.TextArea, data);
        }
    }

    /// <summary>
    /// Команды AvalonEdit, которые ведёт редактор: Ctrl+Z и Ctrl+Y — его отмена, она переносит курсор;
    /// Tab и Shift+Tab в пункте сдвигают пункт; листание — на высоту экрана страницы, а не своего окна.
    /// </summary>
    private void OnTextAreaCommand(object sender, ExecutedRoutedEventArgs e)
    {
        _linesBeforeDelete = e.Command == EditingCommands.Backspace || e.Command == EditingCommands.Delete ? _document.LineCount : null;
        if (e.Command == ApplicationCommands.Undo)
        {
            Undo();
            e.Handled = true;
        }
        else if (e.Command == ApplicationCommands.Redo)
        {
            Redo();
            e.Handled = true;
        }
        else if (e.Command == EditingCommands.TabForward || e.Command == EditingCommands.TabBackward)
        {
            e.Handled = EditingKeys.Shift(_editor.TextArea, outdent: e.Command == EditingCommands.TabBackward);
        }
        else if (_window.IsOpen && Scroller is not null && PageStep(e.Command) is { } step)
        {
            MoveByPage(step.Direction, step.Select);
            e.Handled = true;
        }
    }

    /// <summary>Backspace или Delete удалили строку: нумерованный список у курсора перенумеровывается.</summary>
    private void OnTextAreaCommandDone(object sender, ExecutedRoutedEventArgs e)
    {
        if (_linesBeforeDelete is { } before && _document.LineCount < before)
        {
            EditingKeys.RenumberAfterDelete(_editor.TextArea);
        }

        _linesBeforeDelete = null;
    }

    private static (int Direction, bool Select)? PageStep(ICommand command)
    {
        if (command == EditingCommands.MoveDownByPage)
        {
            return (1, false);
        }

        if (command == EditingCommands.MoveUpByPage)
        {
            return (-1, false);
        }

        if (command == EditingCommands.SelectDownByPage)
        {
            return (1, true);
        }

        return command == EditingCommands.SelectUpByPage ? (-1, true) : null;
    }

    /// <summary>Esc в блочном режиме закрывает окно: набранное остаётся и сразу рисуется.</summary>
    private void OnTextAreaKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && EditMode == MdEditMode.Blocks && _window.IsOpen)
        {
            CloseWindow();
            e.Handled = true;
        }
    }

    /// <summary>
    /// BYDESIGN: за краем своего поля AvalonEdit прижимает мышь к краю, и выделение упирается в окно.
    /// Там конец выделения ставится по месту на странице, а AvalonEdit этого движения не получает.
    /// </summary>
    private void OnTextAreaMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || !_editor.TextArea.IsMouseCaptured)
        {
            return;
        }

        var inside = e.GetPosition(_editor);
        if (inside.Y >= 0 && inside.Y <= _editor.ActualHeight)
        {
            _autoScroll.Stop();
            return;
        }

        ExtendSelectionTo(e.GetPosition(this));
        FollowMouse();
        e.Handled = true;
    }

    /// <summary>Мышь за верхним или нижним краем прокрутки — страница прокручивается сама, пока её там держат.</summary>
    private void FollowMouse()
    {
        if (Scroller is not { } scroller)
        {
            return;
        }

        var y = Mouse.GetPosition(scroller).Y;
        if (y < 0 || y > scroller.ViewportHeight)
        {
            if (!_autoScroll.IsEnabled)
            {
                _autoScroll.Start();
            }
        }
        else
        {
            _autoScroll.Stop();
        }
    }

    private void AutoScroll()
    {
        if (Mouse.LeftButton != MouseButtonState.Pressed || Scroller is not { } scroller)
        {
            EndDrag();
            return;
        }

        var y = Mouse.GetPosition(scroller).Y;
        var beyond = y < 0 ? y : y > scroller.ViewportHeight ? y - scroller.ViewportHeight : 0;
        if (beyond == 0)
        {
            _autoScroll.Stop();
            return;
        }

        scroller.ScrollToVerticalOffset(scroller.VerticalOffset + Math.Clamp(beyond, -AutoScrollStep, AutoScrollStep));
        UpdateLayout();
        ExtendSelectionTo(Mouse.GetPosition(this));
    }

    private void EndDrag()
    {
        _autoScroll.Stop();
        if (_dragging)
        {
            _dragging = false;
            ReleaseMouseCapture();
        }
    }

    /// <summary>Курсор на экран выше или ниже — туда, что окажется под ним; страница сдвигается на экран.</summary>
    private void MoveByPage(int direction, bool select)
    {
        var scroller = Scroller!;
        var area = _editor.TextArea;
        var old = area.Caret.Position;
        var (top, _) = _window.Caret();
        var caret = _window.Element.TranslatePoint(new(area.Caret.CalculateCaretRectangle().X, top), this);
        var target = PositionAt(new(caret.X, Math.Clamp(caret.Y + direction * scroller.ViewportHeight, 0, Math.Max(0, ActualHeight - 1))));
        scroller.ScrollToVerticalOffset(scroller.VerticalOffset + direction * scroller.ViewportHeight);
        if (target is not { } offset)
        {
            return;
        }

        area.Caret.Offset = offset;
        if (select)
        {
            area.Selection = area.Selection.StartSelectionOrSetEndpoint(old, area.Caret.Position);
        }
        else
        {
            area.ClearSelection();
        }
    }

    /// <summary>Место документа под точкой вьюера: в окне — по раскладке AvalonEdit, в отрисовке — начало строки исходника.</summary>
    private int? PositionAt(Point point)
    {
        if (_window.IsOpen && _window.Element.IsDescendantOf(this))
        {
            var local = TranslatePoint(point, _window.Element);
            if (local.Y >= 0 && local.Y < _window.Element.ActualHeight && _window.PositionAt(local) is { } inside)
            {
                return inside;
            }
        }

        if (GetSourceLineAt(point.Y) is not { } line)
        {
            return null;
        }

        var shownLine = (int)Math.Floor(line);
        return OffsetAt(point, shownLine) ?? _document.GetLineByNumber(ToDocumentLine(shownLine)).Offset;
    }

    /// <summary>Место документа под знаком нарисованного текста, если знак стоит на этой строке показанного текста; иначе — <c>null</c>.</summary>
    private int? OffsetAt(Point point, int shownLine)
    {
        var shown = ShownMarkdown;
        return GetPositionFromPoint(point, snapToText: true) is { } position
            && TextMap.SourceOffsetOf(position) is { } offset
            && _shownLines.LineAt(shown, offset) == shownLine
                ? _versions.Map(shown, offset, _document, AnchorMovementType.Default)
                : null;
    }

    private bool IsInWindow(DependencyObject? source)
        => Ancestors(source).TakeWhile(node => !ReferenceEquals(node, this)).Any(node => ReferenceEquals(node, _window.Element));

    /// <summary>Контрол документа, который пишет в исходник: флажок задачи, сворачивание, поле карточки, свой контрол виджета.</summary>
    private bool IsControl(DependencyObject? source)
        => Ancestors(source)
            .TakeWhile(node => !ReferenceEquals(node, this))
            .Any(node => node is MdTaskMark or ToggleButton or TextBoxBase || MdSelection.GetIsInteractive(node));

    private bool OpensWindow(object source)
        => EditMode == MdEditMode.Blocks && !_window.IsOpen && !IsInWindow(source as DependencyObject);

    private void OpenAtCaret()
    {
        _windowWanted = true;
        Reconcile();
        _editor.TextArea.Focus();
    }

    /// <summary>В блочном режиме окно откроется на курсоре: приложение или пользователь поставили его туда.</summary>
    private void WantWindow()
    {
        if (EditMode == MdEditMode.Blocks)
        {
            _windowWanted = true;
        }
    }

    private void QueueReconcile()
    {
        if (_reconcileQueued)
        {
            return;
        }

        _reconcileQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Normal, Reconcile);
    }

    /// <summary>
    /// Окно — на правимых блоках: в полном режиме на всех строках, в блочном — на блоках под курсором и
    /// выделением; пока набирают, только растёт, после разбора втягивает блоки, вылезшие за его край.
    /// Курсор, который подвинули, остаётся на экране.
    /// </summary>
    private void Reconcile()
    {
        _reconcileQueued = false;
        var typed = _typed;
        var caretMoved = _caretMoved;
        var checkEdges = _checkEdges;
        _typed = false;
        _caretMoved = false;
        _checkEdges = false;

        if (WantedLines(typed, caretMoved, checkEdges) is not { } lines)
        {
            CloseWindow();
            return;
        }

        if (!_window.IsOpen || _window.Lines != lines)
        {
            MoveWindow(lines);
        }
        else
        {
            ShowWindow();
            _window.Refit();
            Place();
        }

        if (caretMoved || typed)
        {
            EnsureCaretVisible();
        }
    }

    /// <summary>
    /// Строки документа, на которых должно стоять окно; окна быть не должно — <c>null</c>. Набор окно
    /// растит, переход курсора и отмена ставят заново, а новый разбор втягивает блоки с его краёв.
    /// </summary>
    private (int First, int Last)? WantedLines(bool typed, bool caretMoved, bool checkEdges)
    {
        var after = FrontMatterEditing == MdFrontMatterEditing.Text ? 0 : FrontMatterEnd();
        switch (EditMode)
        {
            case MdEditMode.Full:
                return after < _document.LineCount ? (after + 1, _document.LineCount) : null;
            case MdEditMode.Blocks when _window.IsOpen || _windowWanted:
                break;
            default:
                return null;
        }

        var opening = !_window.IsOpen || _windowWanted;
        _windowWanted = false;
        var area = _editor.TextArea;
        if (area.Caret.Line <= after)
        {
            if (after >= _document.LineCount)
            {
                return null;
            }

            area.ClearSelection();
            area.Caret.Offset = _document.GetLineByNumber(after + 1).Offset;
        }
        var focus = area.Selection.IsEmpty
            ? (area.Caret.Line, area.Caret.Line)
            : (_document.GetLineByOffset(area.Selection.SurroundingSegment.Offset).LineNumber,
                _document.GetLineByOffset(area.Selection.SurroundingSegment.EndOffset).LineNumber);
        var lines = opening || (caretMoved && !typed)
            ? BlockLines(focus)
            : typed
                ? Union(BlockLines(focus), _window.Lines)
                : _window.Lines;

        lines = checkEdges ? BlockLines(lines) : lines;
        return (Math.Max(lines.First, after + 1), Math.Max(lines.Last, after + 1));
    }

    /// <summary>Front matter правится полями или никак: поля карточки — во всех режимах, где правят, текстом — в «только контролах».</summary>
    private void UpdateFrontMatter()
        => SetCurrentValue(
            IsFrontMatterEditableProperty,
            FrontMatterEditing == MdFrontMatterEditing.Fields
                ? EditMode != MdEditMode.View
                : FrontMatterEditing == MdFrontMatterEditing.Text && EditMode == MdEditMode.Controls);

    /// <summary>Последняя строка front matter в документе, с единицы; front matter нет — 0.</summary>
    private int FrontMatterEnd()
    {
        if (_document.LineCount < 2 || _document.GetText(_document.GetLineByNumber(1)).TrimStart('﻿').TrimEnd() != "---")
        {
            return 0;
        }

        for (var line = 2; line <= _document.LineCount; line++)
        {
            if (_document.GetText(_document.GetLineByNumber(line)).TrimEnd() is "---" or "...")
            {
                return line;
            }
        }

        return 0;
    }

    /// <summary>
    /// Строки документа правимых единиц последнего разбора, задевающих строки документа: на самом
    /// глубоком уровне, общем для первой и последней строки, единицы целиком, с соседними строками,
    /// которых вьюер не рисует, а наверху — и до краёв текста.
    /// </summary>
    private (int First, int Last) BlockLines((int First, int Last) lines)
    {
        var shownFirst = ToShownLine(lines.First);
        var shownLast = ToShownLine(lines.Last);
        var (level, top) = CommonLevel(SourceLevelsAt(shownFirst), SourceLevelsAt(shownLast));
        var spans = level.Units;
        if (spans.Count == 0)
        {
            return lines;
        }

        var first = BlockAt(spans, shownFirst);
        var last = BlockAt(spans, shownLast);
        var lower = top ? 0 : spans[0].StartLine;
        var upper = top ? _shownLines.Count(ShownMarkdown) - 1 : spans[^1].EndLine;
        var start = Math.Min(first > 0 ? spans[first - 1].EndLine + 1 : lower, spans[first].StartLine);
        var end = Math.Max(last + 1 < spans.Count ? spans[last + 1].StartLine - 1 : upper, spans[last].EndLine);
        return (Math.Min(lines.First, ToDocumentLine(start)), Math.Max(lines.Last, DocumentLineOfEnd(end)));
    }

    /// <summary>Самый глубокий уровень единиц, общий для двух строк, и верхний ли он.</summary>
    private static (MdSourceLevel Level, bool Top) CommonLevel(IReadOnlyList<MdSourceLevel> first, IReadOnlyList<MdSourceLevel> last)
    {
        var depth = 0;
        while (depth + 1 < first.Count && depth + 1 < last.Count && first[depth + 1].Container == last[depth + 1].Container)
        {
            depth++;
        }

        return (first[depth], depth == 0);
    }

    /// <summary>
    /// Окно встаёт на новые строки: вьюер рисует прежние блоки и прячет новые, ближайшая к курсору строка,
    /// показанная до перехода и после одинаково, остаётся на своём месте экрана, и окно держится там, пока
    /// высоты вокруг уточняются. Отрисовка догоняет текст сразу — прежние блоки рисуются уже по нему.
    /// </summary>
    private void MoveWindow((int First, int Last) lines)
    {
        var steady = SteadyLine(_editor.TextArea.Caret.Line, _window.IsOpen ? _window.Lines : null, lines);
        var before = steady is { } line ? LineViewportTop(line) : null;
        var moved = _window.IsOpen;
        ReleaseSourceWindow();
        _window.Open(lines.First, lines.Last);
        ShowWindow();
        UpdateLayout();
        _window.Refit();
        Place();
        UpdateLayout();

        if (Scroller is { } scroller
            && before is { } screen
            && LineViewportTop(steady!.Value) is { } after
            && Math.Abs(after - screen) > 0.5)
        {
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset + after - screen);
            UpdateLayout();
        }

        if (!_dragging && !_editor.TextArea.IsMouseCaptured)
        {
            HoldSourceWindow();
        }

        if (moved)
        {
            CommitText();
        }
    }

    /// <summary>
    /// Ближайшая к строке курсора строка документа, которую переход окна со строк <paramref name="old"/> на
    /// <paramref name="next"/> показывает одинаково: в окне оба раза или нарисованной оба раза.
    /// </summary>
    private int? SteadyLine(int caret, (int First, int Last)? old, (int First, int Last) next)
    {
        for (var distance = 0; distance < _document.LineCount; distance++)
        {
            int[] candidates = distance == 0 ? [caret] : [caret - distance, caret + distance];
            foreach (var line in candidates)
            {
                if (line >= 1 && line <= _document.LineCount && Inside(old, line) == Inside(next, line))
                {
                    return line;
                }
            }
        }

        return null;

        static bool Inside((int First, int Last)? lines, int line) => lines is { } range && line >= range.First && line <= range.Last;
    }

    /// <summary>
    /// Отдаёт окно вьюеру строками того текста, который он показывает; окно с первой строки документа
    /// начинается с его первой строки, а до последней — кончается его последней.
    /// </summary>
    private void ShowWindow()
    {
        var (first, last) = _window.Lines;
        var shown = ShownMarkdown;
        var start = _versions.ToText(shown, _document.GetLineByNumber(first).Offset, _document, AnchorMovementType.AfterInsertion);
        var end = _versions.ToText(shown, _document.GetLineByNumber(last).EndOffset, _document, AnchorMovementType.BeforeInsertion);
        var shownFirst = first == 1 ? 0 : _shownLines.LineAt(shown, start);
        var shownLast = last == _document.LineCount ? _shownLines.Count(shown) - 1 : Math.Max(shownFirst, _shownLines.LineAt(shown, end));
        PlaceInside(shownFirst, shownLast);
        SetSourceWindow(shownFirst, shownLast, _window.Element, _window);
    }

    /// <summary>
    /// Окно внутри контейнеров: их полосы исходник не рисует — они нарисованы вокруг, а внутри пункта
    /// окно сдвигается влево на отступ, который пункт рисует сам.
    /// </summary>
    private void PlaceInside(int shownFirst, int shownLast)
    {
        var first = SourceLevelsAt(shownFirst);
        var last = SourceLevelsAt(shownLast);
        var depth = 0;
        while (depth + 1 < first.Count && depth + 1 < last.Count && first[depth + 1].Container == last[depth + 1].Container)
        {
            depth++;
        }

        var containers = first
            .Skip(1)
            .Take(depth)
            .Select(level => (level.Container.StartLine, level.Container.EndLine))
            .ToList();
        if (!containers.SequenceEqual(_windowContainers))
        {
            _windowContainers = containers;
            _editor.TextArea.TextView.Redraw();
        }
        var indent = first
            .Take(depth + 1)
            .Max(level => level.Indent);
        _window.Element.Margin = new(-IndentWidth(shownFirst, indent), 0, 0, 0);
    }

    /// <summary>Ширина первых знаков строки показанного текста шрифтом AvalonEdit.</summary>
    private double IndentWidth(int shownLine, int indent)
    {
        var shown = ShownMarkdown;
        var start = _shownLines.StartOf(shown, shownLine);
        var length = Math.Min(indent, _shownLines.EndOf(shown, shownLine) - start);
        if (length <= 0)
        {
            return 0;
        }

        var typeface = new Typeface(_editor.FontFamily, _editor.FontStyle, _editor.FontWeight, _editor.FontStretch);
        return new FormattedText(
                shown.Substring(start, length),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                _editor.FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip)
            .WidthIncludingTrailingWhitespace;
    }

    /// <summary>Окно закрывается: блоки снова нарисованы — уже по набранному.</summary>
    private void CloseWindow()
    {
        if (!_window.IsOpen)
        {
            return;
        }

        if (_editor.TextArea.IsKeyboardFocusWithin)
        {
            Focus();
        }

        EndDrag();
        _window.Close();
        ClearSourceWindow();
        CommitText();
    }

    /// <summary>AvalonEdit — на видимую часть холста окна в прокрутке, где стоит редактор.</summary>
    private void Place()
    {
        if (!_window.IsOpen)
        {
            return;
        }

        if (Scroller is { } scroller && _window.Element.IsDescendantOf(scroller))
        {
            var top = _window.Element.TranslatePoint(default, scroller).Y;
            _window.Place(-top, scroller.ViewportHeight - top);
        }
        else
        {
            _window.Place(0, _window.Height);
        }
    }

    private void QueueRefit()
    {
        if (_refitQueued || !_window.IsOpen)
        {
            return;
        }

        _refitQueued = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            () =>
            {
                _refitQueued = false;
                if (_window.IsOpen)
                {
                    _window.Refit();
                    Place();
                }
            });
    }

    /// <summary>Курсор ушёл за край прокрутки — страница сдвигается к нему.</summary>
    private void EnsureCaretVisible()
    {
        if (!_window.IsOpen || Scroller is not { } scroller || !_window.Element.IsDescendantOf(scroller))
        {
            return;
        }

        var (top, height) = _window.Caret();
        var y = _window.Element.TranslatePoint(new(0, top), scroller).Y;
        var shift = y < 0 ? y - height : y + height > scroller.ViewportHeight ? y + 2 * height - scroller.ViewportHeight : 0;
        if (shift != 0)
        {
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset + shift);
        }
    }

    /// <summary>Верх текста строки документа в видимой области прокрутки: в окне — по AvalonEdit, вне его — по отрисовке.</summary>
    private double? LineViewportTop(int line)
    {
        if (Scroller is not { } scroller || !IsDescendantOf(scroller))
        {
            return null;
        }

        if (_window.IsOpen && _window.Element.IsDescendantOf(scroller) && line >= _window.Lines.First && line <= _window.Lines.Last)
        {
            return _window.Element.TranslatePoint(new(0, _window.TextTop(line) - _window.LineTop(_window.Lines.First)), scroller).Y;
        }

        return GetSourceLineOffset(ToShownLine(line)) is { } y ? TranslatePoint(new(0, y), scroller).Y : null;
    }

    /// <summary>
    /// Куски строки документа с видами — по описанию исходника последнего разбора, места переведены на
    /// нынешний текст. Кусок во всю строку и пустая строка кода берут строку целиком — с набранным у краёв.
    /// </summary>
    private IEnumerable<(int Start, int End, LookStyle Style)> PiecesOn(DocumentLine line)
    {
        var look = _look ??= new(GetSourceLook(), ShownMarkdown, _shownLines);
        var text = look.Text;
        var from = _versions.ToText(text, line.Offset, _document, AnchorMovementType.AfterInsertion);
        var to = _versions.ToText(text, line.EndOffset, _document, AnchorMovementType.BeforeInsertion);
        var first = _shownLines.LineAt(text, from);
        var last = _shownLines.LineAt(text, to);
        for (var shown = first; shown <= last; shown++)
        {
            var (lineStart, lineEnd) = (_shownLines.StartOf(text, shown), _shownLines.EndOf(text, shown));
            var pieces = look.PiecesOn(shown);
            if (first == last && lineStart == lineEnd && pieces.Count == 0 && look.LooksAt(shown).Any(entry => entry.Look == "CodeBlock"))
            {
                if (line.Length > 0)
                {
                    yield return (line.Offset, line.EndOffset, StyleOf("CodeBlock"));
                }

                continue;
            }

            foreach (var (start, end, name) in pieces)
            {
                if (first == last && start <= lineStart && end >= lineEnd && lineEnd > lineStart)
                {
                    if (line.Length > 0)
                    {
                        yield return (line.Offset, line.EndOffset, StyleOf(name));
                    }

                    continue;
                }

                if (end <= from
                    || start >= to
                    || _versions.Map(text, Math.Max(start, from), _document, AnchorMovementType.AfterInsertion) is not { } a
                    || _versions.Map(text, Math.Min(end, to), _document, AnchorMovementType.BeforeInsertion) is not { } b)
                {
                    continue;
                }

                a = Math.Clamp(a, line.Offset, line.EndOffset);
                b = Math.Clamp(b, line.Offset, line.EndOffset);
                if (b > a)
                {
                    yield return (a, b, StyleOf(name));
                }
            }
        }
    }

    /// <summary>
    /// Маркер строки документа — пункта, цитаты: его длина, а у пустой строки цитаты, которая стоит в
    /// отрисовке отступом, — вся строка; маркера нет или строку не узнать — 0.
    /// </summary>
    private (int Length, bool Gap) MarkerOf(DocumentLine line)
    {
        var look = _look ??= new(GetSourceLook(), ShownMarkdown, _shownLines);
        var text = look.Text;
        var shownLine = ToShownLine(line.LineNumber);
        var start = _shownLines.StartOf(text, shownLine);
        if (_versions.Map(text, start, _document, AnchorMovementType.BeforeInsertion) != line.Offset)
        {
            return (0, false);
        }

        var own = _document.GetText(line);
        if (own.Contains('>')
            && IsGapText(own)
            && look.LooksAt(shownLine).Any(entry => !_windowContainers.Contains((entry.FirstLine, entry.LastLine)) && StyleOf(entry.Look).Height is not null))
        {
            return (line.Length, true);
        }

        return look.IndentOn(shownLine) is > 0 and var length
               && _versions.Map(text, start + length, _document, AnchorMovementType.BeforeInsertion) is { } end
            ? (Math.Clamp(end - line.Offset, 0, line.Length), false)
            : (0, false);
    }

    /// <summary>Строка из одних пробелов и знаков цитаты — в отрисовке на её месте отступ.</summary>
    private static bool IsGapText(string text) => text.All(symbol => symbol == '>' || char.IsWhiteSpace(symbol));

    /// <summary>Поля, которые распорка добавляет над текстом строки документа с единицы и под ним.</summary>
    private (double Above, double Below) MarginsOf(int line) => Strut(_document.GetLineByNumber(line)) is { } strut ? (strut.Above, strut.Below) : default;

    /// <summary>
    /// Распорка строки документа: высота строки в отрисовке по полям заголовка и видов, шапке, отступу тела,
    /// высоте пустой строки и полям кода и таблиц; не нужна — <c>null</c>.
    /// </summary>
    private (double Ascent, double Descent, double Above, double Below)? Strut(DocumentLine line)
    {
        var look = _look ??= new(GetSourceLook(), ShownMarkdown, _shownLines);
        var text = look.Text;
        var shownLine = ToShownLine(line.LineNumber);
        if (_versions.Map(text, _shownLines.StartOf(text, shownLine), _document, AnchorMovementType.BeforeInsertion) != line.Offset)
        {
            return null;
        }

        var (top, bottom) = (0.0, 0.0);
        var (family, size) = (_editor.FontFamily, _editor.FontSize);
        foreach (var (_, _, name) in look.PiecesOn(shownLine))
        {
            if (name.StartsWith("Heading", StringComparison.Ordinal) && StyleOf(name) is { Margin: { } margin } heading)
            {
                (top, bottom) = (margin.Top, margin.Bottom);
                (family, size) = (heading.FontFamily ?? family, heading.FontSize ?? size);
            }
            else if (name == "CodeBlock" && StyleOf(name) is var code)
            {
                (family, size) = (code.FontFamily ?? family, code.FontSize ?? size);
            }
        }

        double? blank = null;
        var header = 0.0;
        foreach (var (first, last, name) in look.LooksAt(shownLine))
        {
            if (_windowContainers.Contains((first, last)))
            {
                continue;
            }

            var style = StyleOf(name);
            if (style.Margin is { } margin)
            {
                top = shownLine == first ? Math.Max(top, margin.Top) : top;
                bottom = shownLine == last ? Math.Max(bottom, margin.Bottom) : bottom;
            }

            if (style.BodyMargin is { } body && shownLine == first + 1)
            {
                top = Math.Max(top, body.Top);
            }

            if (style.Header is { } least && shownLine == first)
            {
                header = Math.Max(header, least);
            }

            if (style.Height is { } height && IsGapText(_document.GetText(line)))
            {
                blank = Math.Max(blank ?? 0, height);
            }
        }

        // Шапка в отрисовке — не ниже значка и с округлением раскладки
        if (header > 0)
        {
            var title = family.LineSpacing * size;
            bottom += Rounded(Math.Max(header, title)) - title;
        }

        _fits ??= new SourceFit(
                FindWatched,
                _editor.FontFamily,
                _editor.FontSize,
                _editor.TextArea.TextView.DefaultLineHeight,
                Rounded)
            .Of(look, _shownLines);
        var (above, below) = _fits.GetValueOrDefault(shownLine);
        top += above;
        bottom += below;

        if (blank is { } blankHeight)
        {
            var view = _editor.TextArea.TextView;
            var ascent = Math.Clamp(view.DefaultBaseline - ((view.DefaultLineHeight - blankHeight) / 2), 0, blankHeight);
            return (ascent + top, blankHeight - ascent + bottom, top, bottom);
        }

        return top > 0 || bottom > 0
            ? ((family.Baseline * size) + top, ((family.LineSpacing - family.Baseline) * size) + bottom, top, bottom)
            : null;
    }

    /// <summary>Размер, каким его ставит раскладка отрисовки: с округлением раскладки — до точки экрана.</summary>
    private double Rounded(double value)
    {
        if (!UseLayoutRounding)
        {
            return value;
        }

        var scale = VisualTreeHelper.GetDpi(this).DpiScaleY;
        return Math.Round(value * scale) / scale;
    }

    /// <summary>Разделители столбцов строки документа с местом до и после них; строка не в таблице — пусто.</summary>
    internal IReadOnlyList<(int Offset, double Before, double After)> SeparatorsOf(DocumentLine line)
        => (_separators ??= LayOutTables()).GetValueOrDefault(line.LineNumber) ?? [];

    /// <summary>
    /// Столбцы таблиц описания исходника по нынешнему тексту: край столбца — по самой широкой ячейке, у
    /// столбца по правому краю место встаёт перед текстом, по центру — поровну. Таблица шире AvalonEdit не
    /// выравнивается: перенос строк сломал бы столбцы.
    /// </summary>
    private Dictionary<int, IReadOnlyList<(int Offset, double Before, double After)>> LayOutTables()
    {
        var result = new Dictionary<int, IReadOnlyList<(int Offset, double Before, double After)>>();
        var look = _look ??= new(GetSourceLook(), ShownMarkdown, _shownLines);
        var available = _editor.TextArea.TextView.ActualWidth;
        foreach (var table in look.Columns)
        {
            var rows = new List<(DocumentLine Line, List<int> Separators, List<double> Widths)>();
            foreach (var (shownLine, separators) in table.Rows)
            {
                if (RowOf(look.Text, shownLine, separators) is { } row)
                {
                    rows.Add((row.Line, row.Separators, CellWidths(row.Line, row.Separators)));
                }
            }

            var columns = rows.Count == 0 ? 0 : rows.Max(row => row.Separators.Count);
            var widest = Enumerable.Range(0, columns)
                .Select(column => rows.Where(row => column < row.Widths.Count).Max(row => row.Widths[column]))
                .ToList();
            var separator = rows.Count == 0 ? 0 : MeasureCell(rows[0].Line, rows[0].Separators[0], rows[0].Separators[0] + 1);
            if (rows.Count < 2 || widest.Sum() + (columns * separator) + rows.Max(row => row.Widths[^1]) > available)
            {
                continue;
            }

            foreach (var (line, separators, widths) in rows)
            {
                var spaces = separators.Select(offset => (Offset: offset, Before: 0.0, After: 0.0)).ToList();
                for (var column = 0; column < separators.Count; column++)
                {
                    var extra = widest[column] - widths[column];
                    var alignment = column < table.Alignments.Count ? table.Alignments[column] : TableAlignment.Default;
                    var ahead = column == 0 ? 0 : alignment switch
                    {
                        TableAlignment.Right => extra,
                        TableAlignment.Center => extra / 2,
                        _ => 0
                    };
                    spaces[column] = spaces[column] with { Before = spaces[column].Before + extra - ahead };
                    if (ahead > 0)
                    {
                        spaces[column - 1] = spaces[column - 1] with { After = spaces[column - 1].After + ahead };
                    }
                }

                result[line.LineNumber] = spaces;
            }
        }

        return result;
    }

    /// <summary>Строка таблицы в документе и её разделители там, где они теперь; строку или разделитель не узнать — <c>null</c>.</summary>
    private (DocumentLine Line, List<int> Separators)? RowOf(string text, int shownLine, IReadOnlyList<int> separators)
    {
        if (separators.Count == 0
            || _versions.Map(text, _shownLines.StartOf(text, shownLine), _document, AnchorMovementType.BeforeInsertion) is not { } start
            || _document.GetLineByOffset(Math.Min(start, _document.TextLength)) is not { } line
            || line.Offset != start)
        {
            return null;
        }

        var mapped = new List<int>(separators.Count);
        foreach (var separator in separators)
        {
            if (_versions.Map(text, separator, _document, AnchorMovementType.BeforeInsertion) is not { } at
                || at < line.Offset
                || at >= line.EndOffset
                || _document.GetCharAt(at) != text[separator])
            {
                return null;
            }

            mapped.Add(at);
        }

        return (line, mapped);
    }

    /// <summary>Ширины ячеек строки таблицы — до каждого разделителя и после последнего; та же строка с тем же текстом не перемеряется.</summary>
    private List<double> CellWidths(DocumentLine line, List<int> separators)
    {
        var key = string.Join(",", separators.Select(at => at - line.Offset)) + "|" + _document.GetText(line);
        if (!_cellWidths.TryGetValue(key, out var widths))
        {
            widths = [];
            var start = line.Offset;
            foreach (var separator in separators)
            {
                widths.Add(MeasureCell(line, start, separator));
                start = separator + 1;
            }

            widths.Add(MeasureCell(line, start, line.EndOffset));
            _cellWidths[key] = widths;
        }

        return widths;
    }

    /// <summary>Ширина куска строки документа шрифтом AvalonEdit с оформлением его кусков.</summary>
    private double MeasureCell(DocumentLine line, int start, int end)
    {
        if (end <= start)
        {
            return 0;
        }

        var view = _editor.TextArea.TextView;
        var text = new FormattedText(
            _document.GetText(start, end - start),
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(_editor.FontFamily, _editor.FontStyle, _editor.FontWeight, _editor.FontStretch),
            _editor.FontSize,
            Brushes.Black,
            null,
            TextOptions.GetTextFormattingMode(view),
            VisualTreeHelper.GetDpi(view).PixelsPerDip);
        foreach (var (from, to, style) in PiecesOn(line))
        {
            var a = Math.Max(from, start) - start;
            var b = Math.Min(to, end) - start;
            if (b <= a)
            {
                continue;
            }

            if (style.FontFamily is { } family)
            {
                text.SetFontFamily(family, a, b - a);
            }

            if (style.FontSize is { } size)
            {
                text.SetFontSize(size, a, b - a);
            }

            if (style.FontWeight is { } weight)
            {
                text.SetFontWeight(weight, a, b - a);
            }

            if (style.FontStyle is { } fontStyle)
            {
                text.SetFontStyle(fontStyle, a, b - a);
            }
        }

        return text.WidthIncludingTrailingWhitespace;
    }

    /// <summary>Виды строки документа с единицы: фон, полоса.</summary>
    private IEnumerable<LookStyle> LooksAt(int line)
    {
        var look = _look ??= new(GetSourceLook(), ShownMarkdown, _shownLines);
        return look.LooksAt(ToShownLine(line))
            .Where(entry => !_windowContainers.Contains((entry.FirstLine, entry.LastLine)))
            .Select(entry => StyleOf(entry.Look));
    }

    /// <summary>Шрифт и цвета вида по ключам темы — один раз на разбор.</summary>
    private LookStyle StyleOf(string look)
    {
        if (!_styles.TryGetValue(look, out var style))
        {
            _styles[look] = style = LookStyle.Resolve(look, FindWatched);
        }

        return style;
    }

    /// <summary>Ресурс по ключу темы; ключ остаётся под присмотром, и новое значение перечитывает виды исходника.</summary>
    private object? FindWatched(string key)
    {
        if (_watched.Add(key))
        {
            var watcher = Watchers.GetOrAdd(
                key,
                name => DependencyProperty.RegisterAttached("Watched:" + name, typeof(object), typeof(MarkdownEditor), new PropertyMetadata(null, OnWatchedChanged)));
            _watching = true;
            try
            {
                SetResourceReference(watcher, key);
            }
            finally
            {
                _watching = false;
            }
        }

        return TryFindResource(key);
    }

    private static void OnWatchedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownEditor { _watching: false } editor)
        {
            editor.QueueThemeChange();
        }
    }

    /// <summary>Тема или масштаб экрана сменились: виды, поля и столбцы исходника считаются заново.</summary>
    private void QueueThemeChange()
    {
        if (_themeQueued)
        {
            return;
        }

        _themeQueued = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.Normal,
            () =>
            {
                _themeQueued = false;
                _styles.Clear();
                _fits = null;
                _separators = null;
                _cellWidths.Clear();
                _editor.TextArea.TextView.Redraw();
                QueueRefit();
            });
    }

    /// <summary>Строка показанного текста с нуля — строка документа с единицы, где теперь её начало.</summary>
    private int ToDocumentLine(int shownLine)
    {
        var shown = ShownMarkdown;
        var offset = _shownLines.StartOf(shown, shownLine);
        var mapped = _versions.Map(shown, offset, _document, AnchorMovementType.BeforeInsertion) ?? offset;
        return _document.GetLineByOffset(Math.Clamp(mapped, 0, _document.TextLength)).LineNumber;
    }

    /// <summary>Строка документа с единицы, где теперь конец строки показанного текста с нуля.</summary>
    private int DocumentLineOfEnd(int shownLine)
    {
        var shown = ShownMarkdown;
        var offset = _shownLines.EndOf(shown, shownLine);
        var mapped = _versions.Map(shown, offset, _document, AnchorMovementType.AfterInsertion) ?? offset;
        return _document.GetLineByOffset(Math.Clamp(mapped, 0, _document.TextLength)).LineNumber;
    }

    /// <summary>Строка документа с единицы — строка показанного текста с нуля, где было её начало.</summary>
    private int ToShownLine(int documentLine)
    {
        var shown = ShownMarkdown;
        var offset = _document.GetLineByNumber(Math.Clamp(documentLine, 1, _document.LineCount)).Offset;
        return _shownLines.LineAt(shown, _versions.ToText(shown, offset, _document, AnchorMovementType.BeforeInsertion));
    }

    private void Attach(TextDocument document)
    {
        document.Changed += OnDocumentChanged;
        document.UpdateFinished += OnUpdateFinished;
        document.UndoStack.PropertyChanged += OnUndoStackChanged;
    }

    /// <summary>Приложение поставило AvalonEdit другой документ: окно закрывается, редактор следит за новым, а его текст уходит дальше.</summary>
    private void Reattach()
    {
        if (_editor.Document is not { } document)
        {
            _editor.Document = new();
            return;
        }

        _document.Changed -= OnDocumentChanged;
        _document.UpdateFinished -= OnUpdateFinished;
        _document.UndoStack.PropertyChanged -= OnUndoStackChanged;
        if (_window.IsOpen)
        {
            _window.Close();
            ClearSourceWindow();
        }

        _document = document;
        Attach(_document);
        UpdateUndo();
        Push();
        _dirty = true;
        CommitText();
        QueueReconcile();
    }
}
