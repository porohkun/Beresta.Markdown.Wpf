namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows.Media;
using System.Windows.Media.Imaging;

/// <summary>
/// Загрузки картинок по загрузчику, адресу и ширине декодирования: блок, вернувшийся из-за края
/// видимого, получает уже загруженную картинку сразу. Держит картинки в пределах бюджета памяти, давно
/// не нужные вытесняются первыми. Неудачная загрузка не запоминается, а загрузка, которую больше никто
/// не ждёт, отменяется.
/// </summary>
internal sealed class ImageLoads(long budget)
{
    private readonly object _lock = new();
    private readonly Dictionary<(IMdImageLoader Loader, string Key, int Width), Entry> _entries = [];
    private readonly LinkedList<(IMdImageLoader Loader, string Key, int Width)> _recent = [];
    private long _budget = budget;
    private long _bytes;

    /// <summary>Загрузки всех вьюеров.</summary>
    public static ImageLoads Shared { get; } = new(128L * 1024 * 1024);

    /// <summary>Бюджет памяти в байтах; меньше занятого — лишнее вытесняется сразу.</summary>
    public long Budget
    {
        get
        {
            lock (_lock)
            {
                return _budget;
            }
        }

        set
        {
            lock (_lock)
            {
                _budget = value;
                Trim();
            }
        }
    }

    /// <summary>
    /// Загрузка картинки: уже идущая или законченная — общая, иначе новая. Пока билет не отдан, загрузку
    /// ждут; отдали все — незаконченная отменяется.
    /// </summary>
    public Ticket Request(IMdImageLoader loader, string key, int width, Func<CancellationToken, Task<ImageSource?>> load)
    {
        var id = (loader, key, width);
        Entry entry;
        lock (_lock)
        {
            if (_entries.TryGetValue(id, out var known))
            {
                entry = known;
                _recent.Remove(entry.Node);
                _recent.AddFirst(entry.Node);
            }
            else
            {
                entry = new(_recent.AddFirst(id));
                _entries[id] = entry;
                entry.Loading = Start(load, entry.Cancel.Token);
            }

            entry.Waiting++;
        }

        if (entry.Loading.IsCompleted)
        {
            Complete(entry);
        }
        else
        {
            entry.Loading.ContinueWith(_ => Complete(entry), TaskScheduler.Default);
        }

        return new(this, entry);
    }

    /// <summary>Забывает все загрузки: следующий запрос грузит картинку заново.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            foreach (var entry in _entries.Values.ToList())
            {
                Remove(entry);
            }
        }
    }

    /// <summary>Забывает загрузки адреса — любым загрузчиком и любой шириной.</summary>
    public void Invalidate(string key)
    {
        lock (_lock)
        {
            foreach (var entry in _entries.Where(pair => pair.Key.Key == key).Select(pair => pair.Value).ToList())
            {
                Remove(entry);
            }
        }
    }

    /// <summary>Байты картинки в памяти; не растровая — не в счёт.</summary>
    private static long SizeOf(ImageSource? image)
        => image is BitmapSource bitmap
            ? (long)bitmap.PixelWidth * bitmap.PixelHeight * Math.Max(1, (bitmap.Format.BitsPerPixel + 7) / 8)
            : 0;

    /// <summary>Загрузка; исключение загрузчика — такая же неудачная загрузка.</summary>
    private static Task<ImageSource?> Start(Func<CancellationToken, Task<ImageSource?>> load, CancellationToken token)
    {
        try
        {
            return load(token);
        }
        catch (Exception exception)
        {
            return Task.FromException<ImageSource?>(exception);
        }
    }

    /// <summary>Загрузка кончилась: картинка ложится в счёт памяти, неудача забывается.</summary>
    private void Complete(Entry entry)
    {
        lock (_lock)
        {
            if (entry.Counted || !_entries.TryGetValue(entry.Node.Value, out var current) || !ReferenceEquals(current, entry))
            {
                return;
            }

            if (!entry.Loading.IsCompletedSuccessfully || entry.Loading.Result is null)
            {
                Remove(entry);
                return;
            }

            entry.Counted = true;
            entry.Bytes = SizeOf(entry.Loading.Result);
            _bytes += entry.Bytes;
            Trim();
        }
    }

    /// <summary>Вытесняет давно не нужные картинки, пока занятое больше бюджета.</summary>
    private void Trim()
    {
        for (var node = _recent.Last; node is not null && _bytes > _budget;)
        {
            var previous = node.Previous;
            if (_entries.TryGetValue(node.Value, out var old) && old.Counted)
            {
                Remove(old);
            }

            node = previous;
        }
    }

    /// <summary>Билет больше не ждёт; ждущих не осталось, а загрузка не кончилась — она отменяется.</summary>
    private void Release(Entry entry)
    {
        lock (_lock)
        {
            if (--entry.Waiting > 0 || entry.Loading.IsCompleted)
            {
                return;
            }

            Remove(entry);
        }

        entry.Cancel.Cancel();
    }

    private void Remove(Entry entry)
    {
        if (!_entries.TryGetValue(entry.Node.Value, out var current) || !ReferenceEquals(current, entry))
        {
            return;
        }

        _entries.Remove(entry.Node.Value);
        _recent.Remove(entry.Node);
        _bytes -= entry.Bytes;
    }

    /// <summary>Загрузка, которую ждут, пока билет не отдан.</summary>
    internal sealed class Ticket(ImageLoads loads, Entry entry) : IDisposable
    {
        private bool _released;

        public Task<ImageSource?> Loading => entry.Loading;

        public void Dispose()
        {
            if (!_released)
            {
                _released = true;
                loads.Release(entry);
            }
        }
    }

    internal sealed class Entry(LinkedListNode<(IMdImageLoader Loader, string Key, int Width)> node)
    {
        public LinkedListNode<(IMdImageLoader Loader, string Key, int Width)> Node { get; } = node;

        public CancellationTokenSource Cancel { get; } = new();

        public Task<ImageSource?> Loading { get; set; } = Task.FromResult<ImageSource?>(null);

        public int Waiting { get; set; }

        public bool Counted { get; set; }

        public long Bytes { get; set; }
    }
}
