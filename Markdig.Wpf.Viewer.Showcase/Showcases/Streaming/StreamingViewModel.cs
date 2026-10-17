namespace MarkdigWpfViewer.Showcase.Showcases.Streaming;

using MarkdigWpfViewer.Behaviors;

/// <summary>Длинный ответ, который приходит потоком: куски из фонового потока, вьюер получает их пачками.</summary>
public sealed class StreamingViewModel : ShowcaseViewModel
{
    private static readonly TimeSpan[] Intervals =
    [
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(200),
        TimeSpan.Zero
    ];

    public StreamingViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Поток";

    /// <summary>Ответ, который поддельная модель отдаёт кусками: общий образец и рассказ о потоке вслед.</summary>
    public string Answer { get; } = Samples.Document + "\n\n---\n\n" + Samples.Near<StreamingViewModel>("Answer.md");

    /// <summary>Скорость модели: 0 — как модель, 1 — быстро, 2 — всё разом.</summary>
    public int Speed
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>Выбранный интервал выдачи текста вьюеру: 50, 100, 200 мс или без задержки.</summary>
    public int IntervalChoice
    {
        get;
        set => Set(ref field, value);
    }

    public TimeSpan Interval => Intervals[Math.Clamp(IntervalChoice, 0, Intervals.Length - 1)];

    public StickyEdge Edge
    {
        get;
        set => Set(ref field, value);
    } = StickyEdge.Bottom;

    public bool CanUnstick
    {
        get;
        set => Set(ref field, value);
    } = true;
}
