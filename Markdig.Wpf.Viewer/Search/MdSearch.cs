namespace MarkdigWpfViewer.Search;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Scrolling;
using Selection;

/// <summary>
/// Поиск по тексту вьюера: совпадения — своим слоем пометок, текущее — своим оформлением,
/// «дальше» и «назад» — с прокруткой к нему. Поле ввода и кнопки — у приложения.
/// </summary>
public sealed class MdSearch : INotifyPropertyChanged, IDisposable
{
    /// <summary>Где встаёт совпадение, к которому прокручивает поиск, — доля высоты видимой области сверху.</summary>
    private const double ShowFraction = 0.38;

    private readonly MarkdownViewer _viewer;
    private string _query = string.Empty;
    private bool _matchCase;
    private bool _wholeWord;
    private Brush? _brush;
    private Brush? _currentBrush;
    private IReadOnlyList<MdTextRange> _matches = [];
    private int _current = -1;

    /// <summary>Ставит свой слой пометок поверх слоёв вьюера.</summary>
    public MdSearch(MarkdownViewer viewer)
    {
        _viewer = viewer;
        viewer.MarkLayers.Add(Layer);
        viewer.TextMapChanged += OnTextMapChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Слой с совпадениями во вьюере.</summary>
    public MdMarkLayer Layer { get; } = new("Search");

    /// <summary>Запрос; новый ищется сразу, текущим становится совпадение у прежнего места, и к нему прокручивается.</summary>
    public string Query
    {
        get => _query;
        set => Update(ref _query, value ?? string.Empty);
    }

    public bool MatchCase
    {
        get => _matchCase;
        set => Update(ref _matchCase, value);
    }

    public bool WholeWord
    {
        get => _wholeWord;
        set => Update(ref _wholeWord, value);
    }

    /// <summary>Кисть совпадений; не задана — оформление темы <c>Found</c>.</summary>
    public Brush? Brush
    {
        get => _brush;
        set
        {
            _brush = value;
            UpdateMarks();
            Notify();
        }
    }

    /// <summary>Кисть текущего совпадения; не задана — оформление темы <c>Current</c>.</summary>
    public Brush? CurrentBrush
    {
        get => _currentBrush;
        set
        {
            _currentBrush = value;
            UpdateMarks();
            Notify();
        }
    }

    /// <summary>Совпадения в текстовой карте вьюера по порядку.</summary>
    public IReadOnlyList<MdTextRange> Matches => _matches;

    public int Count => _matches.Count;

    /// <summary>Номер текущего совпадения с нуля; совпадений нет — -1.</summary>
    public int CurrentIndex => _current;

    public MdTextRange? Current => _current >= 0 ? _matches[_current] : null;

    /// <summary>Делает текущим следующее совпадение, после последнего — первое, и прокручивает к нему.</summary>
    public bool Next() => Move(1);

    /// <summary>Делает текущим предыдущее совпадение, перед первым — последнее, и прокручивает к нему.</summary>
    public bool Previous() => Move(-1);

    /// <summary>Убирает свой слой из вьюера.</summary>
    public void Dispose()
    {
        _viewer.TextMapChanged -= OnTextMapChanged;
        _viewer.MarkLayers.Remove(Layer);
    }

    private void Update<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        Notify(name);
        Search(Current?.Start ?? VisiblePosition(), show: true);
    }

    private void OnTextMapChanged(MdTextMap old, MdTextMap map)
    {
        var near = Current is { } current && map.Find(old.Track(current)) is { } moved ? moved.Start : (int?)null;
        Search(near ?? 0, show: false);
    }

    /// <summary>Ищет заново; текущим становится первое совпадение с позиции <paramref name="near"/> или после неё.</summary>
    private void Search(int near, bool show)
    {
        _matches = TextMatches.Find(_viewer.TextMap.Text, _query, _matchCase, _wholeWord);
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

        _current = _current < 0
            ? step > 0 ? FirstFrom(VisiblePosition()) : (FirstFrom(VisiblePosition()) + _matches.Count - 1) % _matches.Count
            : (_current + step + _matches.Count) % _matches.Count;

        UpdateMarks();
        NotifyCurrent();
        Show();
        return true;
    }

    /// <summary>Первое совпадение, которое начинается в позиции или после неё; таких нет — первое.</summary>
    private int FirstFrom(int position)
    {
        for (var i = 0; i < _matches.Count; i++)
        {
            if (_matches[i].Start >= position)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>Позиция карты у верхнего края видимой области; прокрутки нет — начало документа.</summary>
    private int VisiblePosition()
    {
        if (Scrollers.Nearest(_viewer) is not { } scroller)
        {
            return 0;
        }

        var top = scroller.TranslatePoint(default, _viewer);
        return _viewer.GetPositionFromPoint(new(Math.Max(0, top.X), Math.Max(0, top.Y)), snapToText: true) ?? 0;
    }

    /// <summary>Прокручивает к текущему совпадению, если оно не видно целиком.</summary>
    private void Show()
    {
        if (Current is not { } current || IsVisible(current))
        {
            return;
        }

        _viewer.ScrollToRange(current, ShowFraction);
    }

    private bool IsVisible(MdTextRange range)
    {
        var rects = _viewer.GetRangeRects(range);
        if (rects.Count == 0)
        {
            return false;
        }

        if (Scrollers.Nearest(_viewer) is not { } scroller)
        {
            return true;
        }

        var viewport = new Rect(0, 0, scroller.ViewportWidth, scroller.ViewportHeight);
        return rects.All(rect => viewport.Contains(_viewer.TransformToVisual(scroller).TransformBounds(rect)));
    }

    private void UpdateMarks()
    {
        var marks = _matches
            .Select(range => new MdMark(range) { Brush = _brush, Style = _brush is null ? "Found" : null })
            .ToList();

        if (Current is { } current)
        {
            marks.Add(new MdMark(current) { Brush = _currentBrush, Style = _currentBrush is null ? "Current" : null });
        }

        Layer.Marks = marks;
    }

    private void NotifyCurrent()
    {
        Notify(nameof(CurrentIndex));
        Notify(nameof(Current));
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new(name));
}
