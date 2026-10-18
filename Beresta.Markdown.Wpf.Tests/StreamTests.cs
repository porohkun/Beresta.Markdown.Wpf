namespace Beresta.Markdown.Wpf.Tests;

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using System.Windows.Threading;
using Behaviors;
using Streaming;

/// <summary>Поток кусков во вьюер: из любого потока, пачками не чаще интервала, с завершением и сбросом.</summary>
public sealed class StreamTests
{
    private static readonly string[] Words = Enumerable.Range(0, 200).Select(i => i % 20 == 19 ? $"word{i}\n\n" : $"word{i} ").ToArray();

    [Fact]
    public void Куски_из_фонового_потока_доходят_до_вьюера()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer();
            var window = WpfHost.Show(viewer);
            var stream = new MdMarkdownStream(viewer) { Interval = TimeSpan.Zero };

            InBackground(() =>
            {
                foreach (var word in Words)
                {
                    stream.Append(word);
                }

                stream.Complete();
            });
            WpfHost.Settle(window);

            viewer.Markdown.Should().Be(string.Concat(Words));
            stream.IsCompleted.Should().BeTrue();
            window.Close();
        });
    }

    [Fact]
    public void Куски_подряд_ложатся_во_вьюер_одной_пачкой()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer();
            var window = WpfHost.Show(viewer);
            var stream = new MdMarkdownStream(viewer) { Interval = TimeSpan.Zero };
            var updates = 0;
            viewer.TextMapChanged += (_, _) => updates++;

            foreach (var word in Words)
            {
                stream.Append(word);
            }

            WpfHost.Settle(window);

            updates.Should().Be(1);
            viewer.Markdown.Should().Be(string.Concat(Words));
            window.Close();
        });
    }

    [Fact]
    public void Первый_кусок_уходит_сразу_а_следующие_ждут_интервала()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer();
            var window = WpfHost.Show(viewer);
            var stream = new MdMarkdownStream(viewer) { Interval = TimeSpan.FromMilliseconds(300) };

            stream.Append("first ");
            Pump(TimeSpan.FromMilliseconds(100));
            viewer.Markdown.Should().Be("first ", "первый кусок после паузы уходит сразу");

            stream.Append("second");
            Pump(TimeSpan.FromMilliseconds(100));
            viewer.Markdown.Should().Be("first ", "до конца интервала текст ждёт");

            Pump(TimeSpan.FromMilliseconds(400));
            viewer.Markdown.Should().Be("first second");
            window.Close();
        });
    }

    [Fact]
    public void Завершение_отдаёт_остаток_не_дожидаясь_интервала()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer();
            var window = WpfHost.Show(viewer);
            var stream = new MdMarkdownStream(viewer) { Interval = TimeSpan.FromSeconds(5) };
            stream.Append("first ");
            Pump(TimeSpan.FromMilliseconds(100));

            InBackground(() =>
            {
                stream.Append("rest");
                stream.Complete();
            });
            Pump(TimeSpan.FromMilliseconds(200));

            viewer.Markdown.Should().Be("first rest");
            window.Close();
        });
    }

    [Fact]
    public void Завершённый_поток_не_берёт_кусков_пока_его_не_сбросят()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer();
            var window = WpfHost.Show(viewer);
            var stream = new MdMarkdownStream(viewer) { Interval = TimeSpan.Zero };
            stream.Append("first");
            stream.Complete();

            var late = () => stream.Append("late");
            late.Should().Throw<InvalidOperationException>();

            stream.Reset();
            WpfHost.Settle(window);
            viewer.Markdown.Should().BeEmpty();

            stream.Append("second");
            WpfHost.Settle(window);
            viewer.Markdown.Should().Be("second");
            window.Close();
        });
    }

    [Fact]
    public void Сброс_из_потока_вьюера_очищает_его_сразу()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer();
            var window = WpfHost.Show(viewer);
            var stream = new MdMarkdownStream(viewer);
            stream.Append("text");
            WpfHost.Settle(window);

            stream.Reset();

            viewer.Markdown.Should().BeEmpty();
            window.Close();
        });
    }

    [Fact]
    public void Прилипшая_к_низу_прокрутка_идёт_за_концом_потока()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer();
            var scroll = new ScrollViewer { Content = viewer };
            StickyScroll.SetEdge(scroll, StickyEdge.Bottom);
            var window = WpfHost.Show(scroll);
            var stream = new MdMarkdownStream(viewer) { Interval = TimeSpan.Zero };

            for (var i = 0; i < 300; i++)
            {
                stream.Append($"Paragraph {i}\n\n");
                if (i % 50 == 0)
                {
                    WpfHost.Settle(window);
                }
            }

            stream.Complete();
            WpfHost.Settle(window);

            scroll.ScrollableHeight.Should().BePositive();
            scroll.VerticalOffset.Should().BeApproximately(scroll.ScrollableHeight, 1);
            window.Close();
        });
    }

    /// <summary>Выполняет действие в фоновом потоке и ждёт его конца; исключение из потока бросается здесь.</summary>
    private static void InBackground(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });

        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            ExceptionDispatchInfo.Throw(failure);
        }
    }

    /// <summary>Прогоняет очередь потока интерфейса в течение <paramref name="duration"/>.</summary>
    private static void Pump(TimeSpan duration)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < duration)
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
            Dispatcher.PushFrame(frame);
            Thread.Sleep(5);
        }
    }
}
