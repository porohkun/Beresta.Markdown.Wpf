namespace MarkdigWpfViewer.Showcase.Showcases.Streaming;

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Localization;
using MarkdigWpfViewer.Behaviors;
using MarkdigWpfViewer.Streaming;

/// <summary>Поддельная модель в фоновом потоке отдаёт ответ кусками, вьюер показывает его пачками.</summary>
public partial class StreamingView : UserControl
{
    private static readonly DependencyPropertyDescriptor MarkdownDescriptor =
        DependencyPropertyDescriptor.FromProperty(MarkdownViewer.MarkdownProperty, typeof(MarkdownViewer));

    private readonly MdMarkdownStream _stream;
    private readonly DispatcherTimer _statusTimer;
    private CancellationTokenSource? _cancel;
    private Task _producer = Task.CompletedTask;
    private int _chunks;
    private int _updates;

    public StreamingView()
    {
        InitializeComponent();
        _stream = new(Viewer);
        _statusTimer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(200) };
        _statusTimer.Tick += (_, _) => UpdateStatus();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        Localizer.Follow(this, OnLanguageChanged);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        MarkdownDescriptor.AddValueChanged(Viewer, OnMarkdownChanged);
        if (DataContext is StreamingViewModel model)
        {
            model.PropertyChanged += OnModelChanged;
            _stream.Interval = model.Interval;
        }

        _statusTimer.Start();
        UpdateStatus();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _cancel?.Cancel();
        MarkdownDescriptor.RemoveValueChanged(Viewer, OnMarkdownChanged);
        if (DataContext is StreamingViewModel model)
        {
            model.PropertyChanged -= OnModelChanged;
        }

        _statusTimer.Stop();
    }

    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StreamingViewModel.IntervalChoice) && sender is StreamingViewModel model)
        {
            _stream.Interval = model.Interval;
        }
    }

    private void OnMarkdownChanged(object? sender, EventArgs e) => _updates++;

    private async void OnStart(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StreamingViewModel model)
        {
            return;
        }

        _cancel?.Cancel();
        await _producer;

        _cancel = new();
        _stream.Reset();
        _chunks = 0;
        _updates = 0;

        var cancel = _cancel.Token;
        _producer = Task.Run(() => Produce(model.Answer, model.Speed, cancel));
    }

    private void OnStop(object sender, RoutedEventArgs e) => _cancel?.Cancel();

    /// <summary>Язык сменился: поток останавливается, вьюер пустеет до нового запуска.</summary>
    private async void OnLanguageChanged()
    {
        _cancel?.Cancel();
        await _producer;
        _stream.Reset();
        _chunks = 0;
        _updates = 0;
        UpdateStatus();
    }

    /// <summary>
    /// Поддельная модель: куски по нескольку знаков, около 300 знаков в секунду, вдвое быстрее или всё
    /// разом; в конце поток завершается.
    /// </summary>
    private void Produce(string text, int speed, CancellationToken cancel)
    {
        var perSecond = speed switch
        {
            0 => 300.0,
            1 => 600.0,
            _ => double.PositiveInfinity
        };
        var random = new Random();
        var clock = Stopwatch.StartNew();
        var position = 0;
        while (position < text.Length && !cancel.IsCancellationRequested)
        {
            var length = Math.Min(random.Next(2, 7), text.Length - position);
            _stream.Append(text.Substring(position, length));
            position += length;
            Interlocked.Increment(ref _chunks);

            var wait = position * 1000 / perSecond - clock.ElapsedMilliseconds;
            if (wait >= 1)
            {
                Thread.Sleep((int)wait);
            }
        }

        _stream.Complete();
    }

    private void UpdateStatus()
    {
        var edge = StickyScroll.GetEdge(Scroll) == StickyEdge.None
            ? Localizer.Text("Streaming.Status.NotSticking")
            : Localizer.Text(StickyScroll.GetIsStuck(Scroll) ? "Streaming.Status.Stuck" : "Streaming.Status.Unstuck");
        Status.Text = string.Format(
            CultureInfo.CurrentUICulture,
            Localizer.Text("Streaming.Status"),
            Volatile.Read(ref _chunks),
            _updates,
            edge);
    }
}
