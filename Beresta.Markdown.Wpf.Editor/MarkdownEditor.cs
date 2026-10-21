namespace Beresta.Markdown.Wpf.Editor;

using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

/// <summary>
/// A Markdown editor: the viewer that also edits the source of its document. The source lives in
/// AvalonEdit and goes to <see cref="MarkdownViewer.Markdown"/> a moment after the last edit.
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

    private readonly TextEditor _editor = new();
    private readonly SourceVersions _versions = new();
    private readonly DispatcherTimer _commit;
    private TextDocument _document;
    private (string Text, ITextSourceVersion Version)? _given;
    private bool _dirty;
    private long _dirtySince;
    private bool _taking;

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
        _document = _editor.Document;
        Attach(_document);
        _editor.DocumentChanged += (_, _) => Reattach();
        CommandManager.AddPreviewExecutedHandler(_editor.TextArea, OnTextAreaCommand);
    }

    /// <summary>The source changed: typing, an undo, a control, a new text.</summary>
    public event EventHandler<MdSourceChangedEventArgs>? SourceChanged;

    /// <inheritdoc cref="EditModeProperty"/>
    public MdEditMode EditMode
    {
        get => (MdEditMode)GetValue(EditModeProperty);
        set => SetValue(EditModeProperty, value);
    }

    /// <inheritdoc cref="CanUndoProperty"/>
    public bool CanUndo => (bool)GetValue(CanUndoProperty);

    /// <inheritdoc cref="CanRedoProperty"/>
    public bool CanRedo => (bool)GetValue(CanRedoProperty);

    /// <summary>The caret in the source: the position of the character it stands before.</summary>
    public int SourceCaretOffset
    {
        get => _editor.CaretOffset;
        set => _editor.CaretOffset = value;
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

    /// <summary>Selects a piece of the source.</summary>
    public void SelectSource(int start, int length) => _editor.Select(start, length);

    /// <summary>Puts text in place of the selection in the source, or at the caret.</summary>
    public void InsertText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
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

    /// <inheritdoc/>
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        // Щелчок по документу, который правится, переносит фокус в редактор: Ctrl+Z отменяет щелчок по флажку
        if (EditMode != MdEditMode.View && !IsKeyboardFocusWithin)
        {
            Focus();
        }

        base.OnPreviewMouseLeftButtonDown(e);
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

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownEditor)d).Take((string?)e.NewValue ?? string.Empty);

    private static void OnEditModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MarkdownEditor)d;
        var mode = (MdEditMode)e.NewValue;
        editor.SetCurrentValue(AreControlsEditableProperty, mode != MdEditMode.View);
        if (mode == MdEditMode.View)
        {
            editor.CommitText();
        }
    }

    /// <summary>
    /// Новое значение <see cref="MarkdownViewer.Markdown"/>: свой текст, вернувшийся без изменений,
    /// ничего не делает, чужой ложится в документ так, как его принимает AvalonEdit.
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
        }

        _dirty = false;
        _commit.Stop();
        _given = (text, _document.Version);
        _versions.Remember(text, _document.Version);
    }

    /// <summary>Отмена или возврат правки: курсор встаёт туда, где документ поменялся, а текст уходит сразу.</summary>
    private void Step(bool redo)
    {
        var stack = _document.UndoStack;
        if (redo ? !stack.CanRedo : !stack.CanUndo)
        {
            return;
        }

        int? at = null;
        void Track(object? sender, DocumentChangeEventArgs e) => at = e.Offset + e.InsertionLength;

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
        }

        if (at is { } offset)
        {
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
        }

        SourceChanged?.Invoke(this, new(new MdSourceEdit(e.Offset, e.RemovalLength, e.InsertedText.Text)));
    }

    private void OnUndoStackChanged(object? sender, PropertyChangedEventArgs e) => UpdateUndo();

    private void UpdateUndo()
    {
        SetValue(CanUndoPropertyKey, _document.UndoStack.CanUndo);
        SetValue(CanRedoPropertyKey, _document.UndoStack.CanRedo);
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>Ctrl+Z и Ctrl+Y в AvalonEdit — отмена редактора: она переносит курсор.</summary>
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
    }

    private void Attach(TextDocument document)
    {
        document.Changed += OnDocumentChanged;
        document.UndoStack.PropertyChanged += OnUndoStackChanged;
    }

    /// <summary>Приложение поставило AvalonEdit другой документ: редактор следит за ним, а его текст уходит дальше.</summary>
    private void Reattach()
    {
        if (_editor.Document is not { } document)
        {
            _editor.Document = new();
            return;
        }

        _document.Changed -= OnDocumentChanged;
        _document.UndoStack.PropertyChanged -= OnUndoStackChanged;
        _document = document;
        Attach(_document);
        UpdateUndo();
        _dirty = true;
        CommitText();
    }
}
