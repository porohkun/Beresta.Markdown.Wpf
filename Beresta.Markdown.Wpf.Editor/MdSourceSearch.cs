namespace Beresta.Markdown.Wpf.Editor;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Selection;
using ViewModels;

/// <summary>
/// Search and replace in the source of an editor: matches in the document of AvalonEdit, with a background
/// in the edited source and as a mark layer in the drawing; «next» and «previous» go to them. The input
/// fields and the buttons belong to the application.
/// </summary>
public sealed class MdSourceSearch : INotifyPropertyChanged, IDisposable
{
    /// <summary>Где встаёт совпадение, к которому прокручивает поиск, — доля высоты видимой области сверху.</summary>
    private const double ShowFraction = 0.38;

    private readonly MarkdownEditor _editor;
    private readonly SearchBackground _background;
    private string _query = string.Empty;
    private bool _matchCase;
    private bool _wholeWord;
    private IReadOnlyList<MdSourceSpan> _matches = [];
    private int _current = -1;
    private bool _replacing;

    /// <summary>Puts its mark layer over the layers of the editor and its background under the source.</summary>
    public MdSourceSearch(MarkdownEditor editor)
    {
        _editor = editor;
        _background = new(this);
        editor.MarkLayers.Add(Layer);
        editor.TextEditor.TextArea.TextView.BackgroundRenderers.Add(_background);
        editor.SourceChanged += OnSourceChanged;
        editor.DocumentApplied += UpdateMarks;
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The layer with the matches in the drawing.</summary>
    public MdMarkLayer Layer { get; } = new("SourceSearch");

    /// <summary>The query; a new one is searched at once, and the match at the caret or after it becomes current.</summary>
    public string Query
    {
        get => _query;
        set => Update(ref _query, value ?? string.Empty);
    }

    /// <summary>Match case.</summary>
    public bool MatchCase
    {
        get => _matchCase;
        set => Update(ref _matchCase, value);
    }

    /// <summary>Match whole words.</summary>
    public bool WholeWord
    {
        get => _wholeWord;
        set => Update(ref _wholeWord, value);
    }

    /// <summary>Matches in the source in order.</summary>
    public IReadOnlyList<MdSourceSpan> Matches => _matches;

    /// <summary>The number of matches.</summary>
    public int Count => _matches.Count;

    /// <summary>The index of the current match from zero; no matches — -1.</summary>
    public int CurrentIndex => _current;

    /// <summary>The current match; no matches — <c>null</c>.</summary>
    public MdSourceSpan? Current => _current >= 0 ? _matches[_current] : null;

    /// <summary>
    /// Makes the next match current, after the last one — the first: in <see cref="MdEditMode.Blocks"/> and
    /// <see cref="MdEditMode.Full"/> it is selected in the source, otherwise the drawing scrolls to it.
    /// </summary>
    public bool Next() => Move(1);

    /// <summary>Makes the previous match current, before the first one — the last, and goes to it as <see cref="Next"/>.</summary>
    public bool Previous() => Move(-1);

    /// <summary>
    /// Replaces the current match with the text in one step of undo and goes to the next match; in
    /// <see cref="MdEditMode.View"/> or without matches — nothing, <c>false</c>.
    /// </summary>
    public bool Replace(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (_editor.EditMode == MdEditMode.View || Current is not { } current)
        {
            return false;
        }

        Edit(() => _editor.TextEditor.Document.Replace(current.Start, current.Length, text));
        Search(current.Start + text.Length, show: true);
        return true;
    }

    /// <summary>
    /// Replaces every match with the text in one step of undo; in <see cref="MdEditMode.View"/> — nothing.
    /// Returns how many matches were replaced.
    /// </summary>
    public int ReplaceAll(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var matches = _matches;
        if (_editor.EditMode == MdEditMode.View || matches.Count == 0)
        {
            return 0;
        }

        var document = _editor.TextEditor.Document;
        Edit(() =>
        {
            using (document.RunUpdate())
            {
                for (var index = matches.Count - 1; index >= 0; index--)
                {
                    document.Replace(matches[index].Start, matches[index].Length, text);
                }
            }
        });
        Search(0, show: false);
        return matches.Count;
    }

    /// <summary>Removes its layer and background from the editor.</summary>
    public void Dispose()
    {
        _editor.SourceChanged -= OnSourceChanged;
        _editor.DocumentApplied -= UpdateMarks;
        _editor.MarkLayers.Remove(Layer);
        _editor.TextEditor.TextArea.TextView.BackgroundRenderers.Remove(_background);
    }

    private static MdSourceSpan Span(TextDocument document, int start, int length)
        => new(start, length, document.GetLineByOffset(start).LineNumber - 1, document.GetLineByOffset(start + length).LineNumber - 1);

    private void Update<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        Notify(name);
        Search(_editor.SourceCaretOffset, show: true);
    }

    /// <summary>Правка исходника самим поиском: совпадения пересчитываются после неё, а не на каждом её шаге.</summary>
    private void Edit(Action edit)
    {
        _replacing = true;
        try
        {
            edit();
        }
        finally
        {
            _replacing = false;
        }
    }

    /// <summary>Исходник изменился: совпадения ищутся заново, текущим остаётся совпадение на прежнем месте или после него.</summary>
    private void OnSourceChanged(object? sender, MdSourceChangedEventArgs e)
    {
        if (_replacing)
        {
            return;
        }

        var near = Current is { Start: var start } ? start : 0;
        var change = e.Change;
        if (near >= change.Start + change.Length)
        {
            near += change.Text.Length - change.Length;
        }
        else if (near > change.Start)
        {
            near = change.Start;
        }

        Search(near, show: false);
    }

    /// <summary>Ищет заново; текущим становится первое совпадение с места <paramref name="near"/> или после него.</summary>
    private void Search(int near, bool show)
    {
        var document = _editor.TextEditor.Document;
        _matches = SourceMatches.Find(document.Text, _query, _matchCase, _wholeWord)
            .Select(match => Span(document, match.Start, match.Length))
            .ToList();
        _current = _matches.Count == 0 ? -1 : FirstFrom(near);
        UpdateMarks();
        Notify(nameof(Matches));
        Notify(nameof(Count));
        NotifyCurrent();

        if (show)
        {
            Show();
        }
    }

    private bool Move(int step)
    {
        if (_matches.Count == 0)
        {
            return false;
        }

        _current = _current < 0 ? FirstFrom(_editor.SourceCaretOffset) : (_current + step + _matches.Count) % _matches.Count;
        UpdateMarks();
        NotifyCurrent();
        Show();
        return true;
    }

    /// <summary>Первое совпадение, которое начинается в месте или после него; таких нет — первое.</summary>
    private int FirstFrom(int offset)
    {
        for (var index = 0; index < _matches.Count; index++)
        {
            if (_matches[index].Start >= offset)
            {
                return index;
            }
        }

        return 0;
    }

    /// <summary>
    /// К текущему совпадению: в блочном и полном режимах оно выделяется в исходнике, иначе к нему
    /// прокручивается отрисовка.
    /// </summary>
    private void Show()
    {
        if (Current is not { } current)
        {
            return;
        }

        if (_editor.EditMode is MdEditMode.Blocks or MdEditMode.Full)
        {
            _editor.SelectSource(current.Start, current.Length);
        }
        else if (_editor.MapRange(current.Start, current.End) is { } range)
        {
            _editor.ScrollToRange(range, ShowFraction);
        }
    }

    /// <summary>Пометки совпадений, которые видны в отрисовке, и фон совпадений в исходнике.</summary>
    private void UpdateMarks()
    {
        var marks = _matches
            .Select(match => _editor.MapRange(match.Start, match.End))
            .OfType<MdTextRange>()
            .Select(range => new MdMark(range) { Style = "Found" })
            .ToList();
        if (Current is { } current && _editor.MapRange(current.Start, current.End) is { } shown)
        {
            marks.Add(new MdMark(shown) { Style = "Current" });
        }

        Layer.Marks = marks;
        _editor.TextEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    private void NotifyCurrent()
    {
        Notify(nameof(CurrentIndex));
        Notify(nameof(Current));
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new(name));
}
