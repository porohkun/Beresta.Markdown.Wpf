namespace Beresta.Markdown.Wpf.Editor;

using System.ComponentModel;
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
using Scrolling;
using Selection;
using ViewModels;

/// <summary>
/// A Markdown editor: the viewer that also edits the source of its document. The source lives in
/// AvalonEdit and goes to <see cref="MarkdownViewer.Markdown"/> a moment after the last edit; the
/// blocks being edited show it in place of their drawing.
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

    private readonly TextEditor _editor = new();
    private readonly SourceVersions _versions = new();
    private readonly TextLines _shownLines = new();
    private readonly SourceWindow _window;
    private readonly DispatcherTimer _commit;
    private readonly DispatcherTimer _autoScroll;
    private readonly Dictionary<string, LookStyle> _styles = [];
    private SourceLookIndex? _look;
    private TextDocument _document;
    private (string Text, ITextSourceVersion Version)? _given;
    private bool _dirty;
    private long _dirtySince;
    private bool _taking;
    private bool _stepping;
    private bool _typed;
    private bool _caretMoved;
    private bool _checkEdges;
    private bool _windowWanted;
    private bool _reconcileQueued;
    private bool _refitQueued;
    private bool _dragging;

    static MarkdownEditor()
    {
        MarkdownProperty.OverrideMetadata(typeof(MarkdownEditor), new FrameworkPropertyMetadata(OnMarkdownChanged));

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
    }

    /// <summary>Creates an empty editor that only shows.</summary>
    public MarkdownEditor()
    {
        _commit = new(DispatcherPriority.Background, Dispatcher);
        _commit.Tick += (_, _) => CommitText();
        _autoScroll = new(DispatcherPriority.Input, Dispatcher) { Interval = TimeSpan.FromMilliseconds(40) };
        _autoScroll.Tick += (_, _) => AutoScroll();

        SetUpTextEditor();
        _window = new(_editor, ToShownLine, ToDocumentLine);
        _window.Element.SetResourceReference(Panel.BackgroundProperty, "Md.Source.Background");

        _document = _editor.Document;
        Attach(_document);
        _editor.DocumentChanged += (_, _) => Reattach();

        var area = _editor.TextArea;
        area.TextView.LineTransformers.Add(new SourceColorizer(PiecesOn));
        area.TextView.BackgroundRenderers.Add(new SourceBackground(LooksAt));
        CommandManager.AddPreviewExecutedHandler(area, OnTextAreaCommand);
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
        ViewportChanged += (_, _) => Place();
    }

    /// <summary>The source changed: typing, an undo, a control, a new text.</summary>
    public event EventHandler<MdSourceChangedEventArgs>? SourceChanged;

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

    /// <summary>The start of the selection in the source.</summary>
    public int SourceSelectionStart => _editor.SelectionStart;

    /// <summary>The length of the selection in the source; 0 — nothing is selected.</summary>
    public int SourceSelectionLength => _editor.SelectionLength;

    /// <summary>The AvalonEdit that holds the source: its document, options and look.</summary>
    public TextEditor TextEditor => _editor;

    /// <summary>Сколько текст ждёт после последней правки, прежде чем уйти в <see cref="MarkdownViewer.Markdown"/>.</summary>
    internal TimeSpan CommitDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Дольше этого правленый текст не ждёт, даже если правки идут без перерыва.</summary>
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
        _editor.Select(start, length);
    }

    /// <summary>Puts text in place of the selection in the source, or at the caret.</summary>
    public void InsertText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        WantWindow();
        _editor.TextArea.Selection.ReplaceSelectionWithText(text);
    }

    /// <summary>
    /// Hands the edited source to <see cref="MarkdownViewer.Markdown"/> at once, without waiting:
    /// before saving, before reading the text from outside.
    /// </summary>
    public void CommitText()
    {
        _commit.Stop();
        if (!_dirty)
        {
            return;
        }

        _dirty = false;
        var text = _document.Text;
        _given = (text, _document.Version);
        _versions.Remember(text, _document.Version);
        SetCurrentValue(MarkdownProperty, text);
    }

    /// <summary>Undoes the last edit of the source; the caret goes to its place.</summary>
    public void Undo() => Step(redo: false);

    /// <summary>Redoes the last undone edit of the source; the caret goes to its place.</summary>
    public void Redo() => Step(redo: true);

    /// <summary>Строки вне окна, которые AvalonEdit показывает в своей видимой части.</summary>
    internal IReadOnlyList<int> ForeignLines() => _window.ForeignLines();

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
    /// Щелчок по нарисованному: курсор в начало строки исходника под точкой вьюера, и её блок
    /// становится правимым. Под точкой нет строки — <c>false</c>.
    /// </summary>
    internal bool PlaceCaretAt(Point point)
    {
        if (GetSourceLineAt(point.Y) is not { } line)
        {
            return false;
        }

        var target = ToDocumentLine((int)Math.Floor(line));
        if (FrontMatterEditing != MdFrontMatterEditing.Text && target <= FrontMatterEnd())
        {
            return false;
        }

        _windowWanted = true;
        _editor.TextArea.ClearSelection();
        _editor.CaretOffset = _document.GetLineByNumber(target).Offset;
        Reconcile();
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
    protected override void OnDocumentApplied()
    {
        _look = null;
        _styles.Clear();
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

        // BYDESIGN: решение — после раскладки: пересозданный контейнер окна возвращает AvalonEdit с фокусом
        if (e.NewValue is false && EditMode == MdEditMode.Blocks)
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

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownEditor)d).Take((string?)e.NewValue ?? string.Empty);

    private static void OnEditModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownEditor)d).SwitchMode((MdEditMode)e.OldValue, (MdEditMode)e.NewValue);

    private static void OnFrontMatterEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MarkdownEditor)d;
        editor.UpdateFrontMatter();
        editor.QueueReconcile();
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
    /// Новое значение <see cref="MarkdownViewer.Markdown"/>: свой текст, вернувшийся без изменений,
    /// ничего не делает, чужой ложится в документ так, как его принимает AvalonEdit, и закрывает окно.
    /// </summary>
    private void Take(string text)
    {
        if (_given is { } given && string.Equals(text, given.Text, StringComparison.Ordinal))
        {
            _versions.Remember(text, given.Version);
            return;
        }

        if (!string.Equals(text, _document.Text, StringComparison.Ordinal))
        {
            _taking = true;
            try
            {
                _editor.Text = text;
            }
            finally
            {
                _taking = false;
            }

            if (_window.IsOpen)
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
    }

    /// <summary>Отмена или возврат правки: курсор встаёт туда, где документ поменялся, окно — на его блоки, а текст уходит сразу.</summary>
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

    /// <summary>Правка документа: текст уйдёт через секунду после последней правки и не позже чем через пять после первой.</summary>
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
            _typed |= !_stepping;
            QueueReconcile();
        }

        SourceChanged?.Invoke(this, new(new MdSourceEdit(e.Offset, e.RemovalLength, e.InsertedText.Text)));
    }

    private void OnCaretMoved()
    {
        _caretMoved = true;
        QueueReconcile();
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

    /// <summary>
    /// Команды AvalonEdit, которые ведёт редактор: Ctrl+Z и Ctrl+Y — его отмена, она переносит курсор;
    /// Tab и Shift+Tab в пункте сдвигают пункт; листание — на высоту экрана страницы, а не своего окна.
    /// </summary>
    private void OnTextAreaCommand(object sender, ExecutedRoutedEventArgs e)
    {
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

    /// <summary>Esc в блочном режиме закрывает окно: набранное остаётся и уходит в текст.</summary>
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

        return GetSourceLineAt(point.Y) is { } line
            ? _document.GetLineByNumber(ToDocumentLine((int)Math.Floor(line))).Offset
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
    /// Строки документа блоков верхнего уровня последнего разбора, задевающих строки документа:
    /// блоки целиком, с соседними строками, которых вьюер не рисует.
    /// </summary>
    private (int First, int Last) BlockLines((int First, int Last) lines)
    {
        var spans = TopBlockSpans;
        if (spans.Count == 0)
        {
            return lines;
        }

        var shown = ShownMarkdown;
        var first = BlockAt(spans, ToShownLine(lines.First));
        var last = BlockAt(spans, ToShownLine(lines.Last));
        var start = Math.Min(first > 0 ? spans[first - 1].EndLine + 1 : 0, spans[first].StartLine);
        var end = Math.Max(last + 1 < spans.Count ? spans[last + 1].StartLine - 1 : _shownLines.Count(shown) - 1, spans[last].EndLine);
        return (Math.Min(lines.First, ToDocumentLine(start)), Math.Max(lines.Last, DocumentLineOfEnd(end)));
    }

    /// <summary>
    /// Окно встаёт на новые строки: вьюер рисует прежние блоки и прячет новые, строка курсора
    /// возвращается на своё место экрана, и окно держится там, пока высоты вокруг уточняются. Текст
    /// уходит сразу — прежние блоки рисуются уже по нему.
    /// </summary>
    private void MoveWindow((int First, int Last) lines)
    {
        var caretLine = _editor.TextArea.Caret.Line;
        var before = LineViewportTop(caretLine);
        var moved = _window.IsOpen;
        _window.Open(lines.First, lines.Last);
        ShowWindow();
        UpdateLayout();
        _window.Refit();
        Place();
        UpdateLayout();

        if (Scroller is { } scroller
            && before is { } screen
            && screen >= 0
            && screen <= scroller.ViewportHeight
            && LineViewportTop(caretLine) is { } after
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

    /// <summary>Отдаёт окно вьюеру строками того текста, который он показывает.</summary>
    private void ShowWindow()
    {
        var (first, last) = _window.Lines;
        var shown = ShownMarkdown;
        var start = _versions.ToText(shown, _document.GetLineByNumber(first).Offset, _document, AnchorMovementType.AfterInsertion);
        var end = _versions.ToText(shown, _document.GetLineByNumber(last).EndOffset, _document, AnchorMovementType.BeforeInsertion);
        var shownFirst = _shownLines.LineAt(shown, start);
        SetSourceWindow(shownFirst, Math.Max(shownFirst, _shownLines.LineAt(shown, end)), _window.Element, _window);
    }

    /// <summary>Окно закрывается: блоки снова нарисованы, набранное уходит в текст.</summary>
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

    /// <summary>Верх строки документа в видимой области прокрутки: в окне — по AvalonEdit, вне его — по отрисовке.</summary>
    private double? LineViewportTop(int line)
    {
        if (Scroller is not { } scroller || !IsDescendantOf(scroller))
        {
            return null;
        }

        if (_window.IsOpen && _window.Element.IsDescendantOf(scroller) && line >= _window.Lines.First && line <= _window.Lines.Last)
        {
            return _window.Element.TranslatePoint(new(0, _window.LineTop(line) - _window.LineTop(_window.Lines.First)), scroller).Y;
        }

        return GetSourceLineOffset(ToShownLine(line)) is { } y ? TranslatePoint(new(0, y), scroller).Y : null;
    }

    /// <summary>Куски строки документа с видами — по описанию исходника последнего разбора, места переведены на нынешний текст.</summary>
    private IEnumerable<(int Start, int End, LookStyle Style)> PiecesOn(DocumentLine line)
    {
        var look = _look ??= new(GetSourceLook(), ShownMarkdown, _shownLines);
        var text = look.Text;
        var from = _versions.ToText(text, line.Offset, _document, AnchorMovementType.AfterInsertion);
        var to = _versions.ToText(text, line.EndOffset, _document, AnchorMovementType.BeforeInsertion);
        var last = _shownLines.LineAt(text, to);
        for (var shown = _shownLines.LineAt(text, from); shown <= last; shown++)
        {
            foreach (var (start, end, name) in look.PiecesOn(shown))
            {
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

    /// <summary>Виды строки документа с единицы: фон, полоса.</summary>
    private IEnumerable<LookStyle> LooksAt(int line)
    {
        var look = _look ??= new(GetSourceLook(), ShownMarkdown, _shownLines);
        return look.LooksAt(ToShownLine(line)).Select(StyleOf);
    }

    /// <summary>Шрифт и цвета вида по ключам темы — один раз на разбор.</summary>
    private LookStyle StyleOf(string look)
    {
        if (!_styles.TryGetValue(look, out var style))
        {
            _styles[look] = style = LookStyle.Resolve(look, key => TryFindResource(key));
        }

        return style;
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
        _document.UndoStack.PropertyChanged -= OnUndoStackChanged;
        if (_window.IsOpen)
        {
            _window.Close();
            ClearSourceWindow();
        }

        _document = document;
        Attach(_document);
        UpdateUndo();
        _dirty = true;
        CommitText();
        QueueReconcile();
    }
}
