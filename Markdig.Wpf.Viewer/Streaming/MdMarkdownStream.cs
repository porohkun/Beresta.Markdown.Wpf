namespace MarkdigWpfViewer.Streaming;

using System.Text;
using System.Windows.Threading;

/// <summary>
/// Текст, который приходит кусками, — ответ модели или сети — во вьюере: куски принимаются из любого
/// потока, а вьюер получает накопленный текст пачками не чаще раза в <see cref="Interval"/>.
/// </summary>
public sealed class MdMarkdownStream
{
    private readonly MarkdownViewer _viewer;
    private readonly StringBuilder _text = new();
    private readonly Lock _gate = new();
    private TimeSpan _interval = TimeSpan.FromMilliseconds(100);
    private long _lastFlush;
    private bool _flushQueued;
    private bool _isCompleted;

    public MdMarkdownStream(MarkdownViewer viewer)
    {
        _viewer = viewer;
    }

    /// <summary>
    /// Наименьшее время между выдачами текста вьюеру, по умолчанию 100 мс; ноль — как можно скорее.
    /// Первый кусок после паузы, <see cref="Complete"/> и <see cref="Reset"/> уходят сразу.
    /// </summary>
    public TimeSpan Interval
    {
        get
        {
            lock (_gate)
            {
                return _interval;
            }
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            lock (_gate)
            {
                _interval = value;
            }
        }
    }

    /// <summary>Весь полученный текст.</summary>
    public string Text
    {
        get
        {
            lock (_gate)
            {
                return _text.ToString();
            }
        }
    }

    /// <summary>Поток завершён: новые куски не принимаются до <see cref="Reset"/>.</summary>
    public bool IsCompleted
    {
        get
        {
            lock (_gate)
            {
                return _isCompleted;
            }
        }
    }

    /// <summary>Дописывает кусок текста; зовётся из любого потока.</summary>
    public void Append(string chunk)
    {
        lock (_gate)
        {
            if (_isCompleted)
            {
                throw new InvalidOperationException("The stream is completed; call Reset to start a new one.");
            }

            _text.Append(chunk);
            if (!_flushQueued)
            {
                _flushQueued = true;
                Post(_lastFlush + (long)_interval.TotalMilliseconds - Environment.TickCount64);
            }
        }
    }

    /// <summary>Завершает поток: остаток текста уходит во вьюер сразу.</summary>
    public void Complete()
    {
        lock (_gate)
        {
            _isCompleted = true;
        }

        FlushNow();
    }

    /// <summary>Начинает заново: вьюер пустеет, поток снова принимает куски.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            _text.Clear();
            _isCompleted = false;
        }

        FlushNow();
    }

    /// <summary>Отдаёт текст вьюеру без ожидания интервала; в потоке вьюера — прямо сейчас.</summary>
    private void FlushNow()
    {
        if (_viewer.Dispatcher.CheckAccess())
        {
            Flush();
            return;
        }

        Post(0);
    }

    /// <summary>Ставит выдачу текста в очередь вьюера через <paramref name="wait"/> миллисекунд.</summary>
    private void Post(long wait)
    {
        if (wait > 0)
        {
            Task.Delay(TimeSpan.FromMilliseconds(wait)).ContinueWith(_ => Post(0), TaskScheduler.Default);
            return;
        }

        _viewer.Dispatcher.BeginInvoke(DispatcherPriority.Render, Flush);
    }

    private void Flush()
    {
        string text;
        lock (_gate)
        {
            _flushQueued = false;
            _lastFlush = Environment.TickCount64;
            text = _text.ToString();
        }

        _viewer.SetCurrentValue(MarkdownViewer.MarkdownProperty, text);
    }
}
