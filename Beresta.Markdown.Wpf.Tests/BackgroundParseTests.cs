namespace Beresta.Markdown.Wpf.Tests;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Markdig.Syntax;
using Scrolling;
using Streaming;
using ViewModels;

/// <summary>Разбор длинного текста вне потока интерфейса: порог, схлопывание правок, откат в поток интерфейса.</summary>
public sealed class BackgroundParseTests
{
    [Fact]
    public void Текст_короче_порога_разбирается_сразу()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { BackgroundParseThreshold = 1000 };
            var window = WpfHost.Show(viewer);

            viewer.Markdown = "short text";

            viewer.TextMap.Text.Should().Be("short text");
            window.Close();
        });
    }

    [Fact]
    public void Текст_от_порога_разбирается_в_фоне_и_ложится_в_потоке_интерфейса()
    {
        WpfHost.Invoke(() =>
        {
            var threads = new List<int>();
            var viewer = new MarkdownViewer { BackgroundParseThreshold = 5, Extensions = { Recording(threads) } };
            var window = WpfHost.Show(viewer);

            viewer.Markdown = "long enough text";
            viewer.TextMap.Text.Should().BeEmpty();

            WaitFor(window, () => viewer.TextMap.Text == "long enough text");
            viewer.TextMap.Text.Should().Be("long enough text");
            threads.Should().NotBeEmpty().And.NotContain(Environment.CurrentManagedThreadId);
            window.Close();
        });
    }

    [Fact]
    public void Правки_во_время_разбора_схлопываются_в_последнюю()
    {
        WpfHost.Invoke(() =>
        {
            var parsed = new List<string>();
            var viewer = new MarkdownViewer { BackgroundParseThreshold = 1, Extensions = { Collecting(parsed) } };
            var window = WpfHost.Show(viewer);
            var shown = new List<string>();
            DependencyPropertyDescriptor.FromProperty(MarkdownViewer.TextMapProperty, typeof(MarkdownViewer))
                .AddValueChanged(viewer, (_, _) => shown.Add(viewer.TextMap.Text));

            viewer.Markdown = "first";
            viewer.Markdown = "second";
            viewer.Markdown = "third";
            WaitFor(window, () => viewer.TextMap.Text == "third");

            parsed.Should().Equal("first", "third");
            shown.Should().Equal("first", "third");
            window.Close();
        });
    }

    [Fact]
    public void Разбор_упавший_в_фоне_повторяется_в_потоке_интерфейса()
    {
        WpfHost.Invoke(() =>
        {
            var extension = InterfaceThreadOnly();
            var viewer = new MarkdownViewer { BackgroundParseThreshold = 1, Extensions = { extension } };
            var failures = new List<MdRenderFailedEventArgs>();
            viewer.RenderFailed += (_, e) => failures.Add(e);
            var window = WpfHost.Show(viewer);

            viewer.Markdown = "needs the interface thread";
            WaitFor(window, () => viewer.TextMap.Text.Length > 0);

            viewer.TextMap.Text.Should().Be("needs the interface thread");
            failures.Should().ContainSingle().Which.Should().Match<MdRenderFailedEventArgs>(failure =>
                failure.Extension == extension && failure.IsBackground);
            window.Close();
        });
    }

    [Fact]
    public void Расширение_упавшее_и_в_фоне_и_в_потоке_интерфейса_даёт_одно_событие()
    {
        WpfHost.Invoke(() =>
        {
            var failing = new LambdaExtension { Block = (block, _) => block is ParagraphBlock ? throw new InvalidOperationException("block") : null };
            var viewer = new MarkdownViewer { BackgroundParseThreshold = 1, Extensions = { failing } };
            var failures = new List<MdRenderFailedEventArgs>();
            viewer.RenderFailed += (_, e) => failures.Add(e);
            var unhandled = new List<Exception>();
            DispatcherUnhandledExceptionEventHandler handler = (_, e) =>
            {
                unhandled.Add(e.Exception);
                e.Handled = true;
            };
            viewer.Dispatcher.UnhandledException += handler;
            var window = WpfHost.Show(viewer);

            viewer.Markdown = "text **bold**";
            WaitFor(window, () => viewer.TextMap.Text.Length > 0);

            viewer.Dispatcher.UnhandledException -= handler;
            viewer.TextMap.Text.Should().Be("text bold");
            failures.Should().ContainSingle().Which.Should().Match<MdRenderFailedEventArgs>(failure =>
                failure.Extension == failing && !failure.IsBackground);
            unhandled.Should().BeEmpty();
            window.Close();
        });
    }

    [Fact]
    public void Вложенность_которую_Markdig_не_разбирает_в_фоне_показывается_строками_исходника()
    {
        WpfHost.Invoke(() =>
        {
            var line = string.Concat(Enumerable.Repeat("- ", 150)) + "deep";
            var viewer = new MarkdownViewer { BackgroundParseThreshold = 1 };
            var failures = new List<MdRenderFailedEventArgs>();
            viewer.RenderFailed += (_, e) => failures.Add(e);
            var window = WpfHost.Show(viewer);

            viewer.Markdown = line;
            WaitFor(window, () => viewer.TextMap.Text.Length > 0);

            viewer.TextMap.Text.Should().Be(line);
            failures.Should().ContainSingle().Which.Should().Match<MdRenderFailedEventArgs>(failure =>
                failure.Exception is ArgumentException && failure.Extension == null && !failure.IsBackground);
            window.Close();
        });
    }

    [Fact]
    public void Разметка_выделенного_берётся_из_показанного_текста()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { BackgroundParseThreshold = 1 };
            var window = WpfHost.Show(viewer);
            viewer.Markdown = "**shown** text";
            WaitFor(window, () => viewer.TextMap.Text == "shown text");
            viewer.SelectAll();

            viewer.Markdown = "*another* document that is still being parsed";

            viewer.SelectedMarkdown.Should().Be("**shown** text");
            window.Close();
        });
    }

    [Fact]
    public void Поток_текста_доходит_до_вьюера_с_фоновым_разбором()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { BackgroundParseThreshold = 10 };
            var window = WpfHost.Show(viewer);
            var stream = new MdMarkdownStream(viewer) { Interval = TimeSpan.Zero };
            var words = Enumerable.Range(0, 50).Select(i => $"word{i} ").ToList();

            foreach (var word in words)
            {
                stream.Append(word);
            }

            stream.Complete();
            var expected = string.Concat(words).TrimEnd();
            WaitFor(window, () => viewer.TextMap.Text == expected);
            viewer.TextMap.Text.Should().Be(expected);
            window.Close();
        });
    }

    [Fact]
    public void Две_панели_встают_вровень_с_фоновым_разбором()
    {
        WpfHost.Invoke(() =>
        {
            var left = new MarkdownViewer { BackgroundParseThreshold = 1 };
            var right = new MarkdownViewer { BackgroundParseThreshold = 1 };
            Grid.SetColumn(right, 1);
            var window = WpfHost.Show(new ScrollViewer { Content = new Grid { ColumnDefinitions = { new(), new() }, Children = { left, right } } });
            using var aligner = new MdPaneAligner(left, right, new([(0, 0), (1, 1), (2, 5), (3, 6), (4, 7), (5, 8)]));

            left.Markdown = "first\n\nsecond\n\nthird";
            right.Markdown = "first\n\nadded\nparagraph\n\nsecond\n\nthird";
            WaitFor(window, () => left.TextMap.Text.Length > 0 && right.TextMap.Text.Length > 0);
            aligner.Align();
            WpfHost.Settle(window);

            left.Gaps.Should().ContainSingle().Which.Line.Should().Be(2);
            left.GetSourceLineOffset(4).Should().BeApproximately(right.GetSourceLineOffset(7)!.Value, 0.5);
            window.Close();
        });
    }

    /// <summary>Прогоняет очередь окна, пока условие не выполнится; фоновая работа успевает за это время.</summary>
    private static void WaitFor(Window window, Func<bool> condition)
    {
        for (var attempt = 0; attempt < 300 && !condition(); attempt++)
        {
            Thread.Sleep(10);
            WpfHost.Settle(window);
        }
    }

    /// <summary>Расширение, которое запоминает потоки, где разбирались абзацы.</summary>
    private static LambdaExtension Recording(List<int> threads) => new()
    {
        Block = (block, _) =>
        {
            if (block is ParagraphBlock)
            {
                lock (threads)
                {
                    threads.Add(Environment.CurrentManagedThreadId);
                }
            }

            return null;
        }
    };

    /// <summary>Расширение, которое запоминает тексты разобранных абзацев.</summary>
    private static LambdaExtension Collecting(List<string> texts) => new()
    {
        Block = (block, context) =>
        {
            if (block is ParagraphBlock paragraph)
            {
                lock (texts)
                {
                    texts.Add(context.GetSource(paragraph));
                }
            }

            return null;
        }
    };

    /// <summary>Расширение, которое работает только в потоке интерфейса, как код, что трогает его объекты.</summary>
    private static LambdaExtension InterfaceThreadOnly() => new()
    {
        Block = (block, _) => Application.Current.Dispatcher.CheckAccess()
            ? null
            : throw new InvalidOperationException("Not the interface thread.")
    };
}
