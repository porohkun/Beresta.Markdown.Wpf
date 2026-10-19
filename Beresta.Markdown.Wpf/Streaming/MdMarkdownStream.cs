namespace Beresta.Markdown.Wpf.Streaming;

using System.Text;
using System.Windows.Threading;

/// <summary>
/// Text that comes in chunks — the answer of a model or of the network — in the viewer: chunks are
/// taken from any thread, and the viewer gets the accumulated text in batches no more often than
/// once per <see cref="Interval"/>.
/// </summary>
public sealed class MdMarkdownStream
{
    private readonly MarkdownViewer _viewer;
    private readonly StringBuilder _text = new();
    // BYDESIGN: object, а не Lock — Lock нет в .NET 8.
    private readonly object _gate = new();
    private TimeSpan _interval = TimeSpan.FromMilliseconds(100);
    private long _lastFlush;
    private bool _flushQueued;
    private bool _isCompleted;

    /// <summary>A stream of text into a viewer.</summary>
    public MdMarkdownStream(MarkdownViewer viewer)
    {
        _viewer = viewer;
    }

    /// <summary>
    /// The shortest time between handing text to the viewer, 100 ms by default; zero — as soon as
    /// possible. The first chunk after a pause, <see cref="Complete"/> and <see cref="Reset"/> go
    /// at once.
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
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The interval must not be negative.");
            }

            lock (_gate)
            {
                _interval = value;
            }
        }
    }

    /// <summary>All the received text.</summary>
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

    /// <summary>The stream is complete: new chunks are not taken until <see cref="Reset"/>.</summary>
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

    /// <summary>Appends a chunk of text; called from any thread.</summary>
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

    /// <summary>Completes the stream: the rest of the text goes to the viewer at once.</summary>
    public void Complete()
    {
        lock (_gate)
        {
            _isCompleted = true;
        }

        FlushNow();
    }

    /// <summary>Starts over: the viewer empties, the stream takes chunks again.</summary>
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
