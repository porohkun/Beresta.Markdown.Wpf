namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Presenters;

/// <summary>
/// Вьюер создаёт видимые блоки и запас в полэкрана сверху и снизу, где бы ни стояла прокрутка.
/// </summary>
public sealed class VirtualizationTests
{
    private const double CacheShare = 0.5;

    private static readonly string LongText = string.Join(
        "\n\n",
        Enumerable.Range(0, 2000).Select(i => $"Paragraph {i}"));

    private static readonly string TallText = string.Join(
        "\n\n",
        Enumerable.Range(0, 300).Select(i => CodeBlock(i, 10)));

    [Fact]
    public void Во_внешней_прокрутке_создаются_только_видимые_блоки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = LongText };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            ShouldRealizeVisibleOnly(viewer);
            window.Close();
        });
    }

    [Fact]
    public void Высокие_блоки_создаются_только_видимые()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = TallText };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            ShouldRealizeVisibleOnly(viewer);
            window.Close();
        });
    }

    [Fact]
    public void После_прокрутки_создаются_блоки_нового_места()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = LongText };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);

            scroll.ScrollToVerticalOffset(scroll.ExtentHeight / 2);
            WpfHost.Settle(window);

            viewer.ItemContainerGenerator.ContainerFromIndex(0).Should().BeNull();
            ShouldRealizeVisibleOnly(viewer);
            window.Close();
        });
    }

    [Fact]
    public void Конец_документа_достижим()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = LongText };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);

            scroll.ScrollToEnd();
            WpfHost.Settle(window);
            scroll.ScrollToEnd();
            WpfHost.Settle(window);

            viewer.ItemContainerGenerator.ContainerFromIndex(viewer.Items.Count - 1).Should().NotBeNull();
            ShouldRealizeVisibleOnly(viewer);
            window.Close();
        });
    }

    [Fact]
    public void Рядом_с_другим_содержимым_в_общей_прокрутке_создаются_только_видимые_блоки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = LongText };
            var page = new StackPanel();
            page.Children.Add(new Border { Height = 300 });
            page.Children.Add(viewer);
            var window = WpfHost.Show(new ScrollViewer { Content = page });

            ShouldRealizeVisibleOnly(viewer);
            window.Close();
        });
    }

    [Fact]
    public void В_виртуализированном_списке_вьюеры_создают_только_видимые_блоки()
    {
        WpfHost.Invoke(() =>
        {
            var message = string.Join("\n\n", Enumerable.Range(0, 200).Select(i => $"Line {i}"));
            var list = new ListBox();
            var viewers = Enumerable.Range(0, 30)
                .Select(_ => new MarkdownViewer { Markdown = message })
                .ToList();
            viewers.ForEach(viewer => list.Items.Add(viewer));
            var window = WpfHost.Show(list);

            var loaded = viewers.Where(viewer => viewer.IsLoaded).ToList();
            loaded.Should().HaveCountLessThan(viewers.Count);
            loaded.Sum(Realized).Should().BePositive();
            loaded.ForEach(ShouldRealizeVisibleOnly);
            window.Close();
        });
    }

    [Fact]
    public void Заниженная_оценка_высоты_не_оставляет_лишних_блоков()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join(
                "\n\n",
                Enumerable.Range(0, 400).Select(i => i < 200 ? $"Paragraph {i}" : CodeBlock(i, 10)));
            var viewer = new MarkdownViewer { Markdown = text };
            var list = new ListBox();
            VirtualizingPanel.SetScrollUnit(list, ScrollUnit.Pixel);
            list.Items.Add(viewer);
            var window = WpfHost.Show(list);

            var scroll = (ScrollViewer)VisualTreeHelper.GetChild((Border)VisualTreeHelper.GetChild(list, 0), 0);
            scroll.ScrollToVerticalOffset(scroll.ExtentHeight * 0.75);
            WpfHost.Settle(window);

            ShouldRealizeVisibleOnly(viewer);
            window.Close();
        });
    }

    [Theory]
    [InlineData("> ")]
    [InlineData("- ")]
    public void Длинное_вложенное_содержимое_тоже_виртуализируется(string prefix)
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n" + prefix.TrimEnd() + "\n", Enumerable.Range(0, 2000).Select(i => $"{prefix}Line {i}"));
            var viewer = new MarkdownViewer { Markdown = text };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            viewer.Items.Count.Should().Be(1);
            Visuals.Descendants<MdTextBlock>(viewer).Count().Should().BeInRange(1, 100);
            window.Close();
        });
    }

    [Fact]
    public void Без_прокрутки_создаются_все_блоки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "a\n\nb\n\nc" };
            var window = WpfHost.Show(viewer);

            Realized(viewer).Should().Be(viewer.Items.Count);
            window.Close();
        });
    }

    [Fact]
    public void Видимое_не_прыгает_при_прокрутке_вверх_после_прыжка_в_середину()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join(
                "\n\n",
                Enumerable.Range(0, 600).Select(i => i % 5 == 0 ? CodeBlock(i, 10) : $"Paragraph {i}"));
            var viewer = new MarkdownViewer { Markdown = text };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);

            scroll.ScrollToVerticalOffset(scroll.ExtentHeight / 2);
            WpfHost.Settle(window);

            for (var step = 0; step < 10; step++)
            {
                var anchor = Spans(viewer).First(span => span.Bottom > ViewportOf(viewer).Top).Index;
                var before = TopOf(viewer, anchor, scroll);

                scroll.ScrollToVerticalOffset(scroll.VerticalOffset - 48);
                WpfHost.Settle(window);

                TopOf(viewer, anchor, scroll).Should().BeApproximately(before + 48, 1, $"шаг {step}");
            }

            window.Close();
        });
    }

    [Fact]
    public void Новый_текст_пересобирает_блоки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = LongText };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            scroll.ScrollToVerticalOffset(scroll.ExtentHeight / 2);
            WpfHost.Settle(window);

            viewer.Markdown = "a\n\nb";
            WpfHost.Settle(window);

            viewer.Items.Count.Should().Be(3);
            Realized(viewer).Should().Be(3);
            window.Close();
        });
    }

    /// <summary>
    /// Созданные блоки идут подряд и покрывают видимую часть вьюера целиком, а за видимую
    /// область выходят не дальше запаса.
    /// </summary>
    private static void ShouldRealizeVisibleOnly(MarkdownViewer viewer)
    {
        var panel = PanelOf(viewer);
        var viewport = ViewportOf(viewer);
        var cache = (viewport.Bottom - viewport.Top) * CacheShare;
        var spans = Spans(viewer);

        spans.Where(span => span.Bottom <= viewport.Top - cache - 1 || span.Top >= viewport.Bottom + cache + 1)
            .Should().BeEmpty("созданное не выходит за видимую область дальше запаса");

        var visibleTop = Math.Max(viewport.Top, 0);
        var visibleBottom = Math.Min(viewport.Bottom, panel.ActualHeight);
        if (visibleBottom <= visibleTop)
        {
            return;
        }

        spans.Should().NotBeEmpty("видимая часть вьюера не пуста");
        spans.Select(span => span.Index).Should().BeInAscendingOrder()
            .And.HaveCount(spans[^1].Index - spans[0].Index + 1, "созданные блоки идут подряд");
        spans[0].Top.Should().BeLessThanOrEqualTo(visibleTop + 1, "видимое сверху создано");
        spans[^1].Bottom.Should().BeGreaterThanOrEqualTo(visibleBottom - 1, "видимое снизу создано");
    }

    /// <summary>Пересечение видимых областей всех прокруток-предков в координатах панели.</summary>
    private static (double Top, double Bottom) ViewportOf(MarkdownViewer viewer)
    {
        var panel = PanelOf(viewer);
        var top = double.NegativeInfinity;
        var bottom = double.PositiveInfinity;

        for (var node = VisualTreeHelper.GetParent(panel); node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is ScrollViewer scroll)
            {
                var bounds = scroll.TransformToDescendant(panel)
                    .TransformBounds(new(0, 0, scroll.ActualWidth, scroll.ActualHeight));
                top = Math.Max(top, bounds.Top);
                bottom = Math.Min(bottom, bounds.Bottom);
            }
        }

        return (top, bottom);
    }

    private static List<(int Index, double Top, double Bottom)> Spans(MarkdownViewer viewer)
    {
        var panel = PanelOf(viewer);

        return Enumerable.Range(0, viewer.Items.Count)
            .Select(index => (Index: index, Container: viewer.ItemContainerGenerator.ContainerFromIndex(index)))
            .Where(item => item.Container is FrameworkElement)
            .Select(item =>
            {
                var container = (FrameworkElement)item.Container;
                var top = container.TransformToAncestor(panel).Transform(new(0, 0)).Y;
                return (item.Index, top, top + container.ActualHeight);
            })
            .ToList();
    }

    private static MdBlockPanel PanelOf(DependencyObject root)
    {
        return FindPanel(root) ?? throw new InvalidOperationException("У вьюера нет панели блоков.");
    }

    private static MdBlockPanel? FindPanel(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if ((child as MdBlockPanel ?? FindPanel(child)) is { } panel)
            {
                return panel;
            }
        }

        return null;
    }

    private static int Realized(MarkdownViewer viewer)
    {
        return Enumerable.Range(0, viewer.Items.Count)
            .Count(index => viewer.ItemContainerGenerator.ContainerFromIndex(index) is not null);
    }

    private static double TopOf(MarkdownViewer viewer, int index, ScrollViewer scroll)
    {
        var container = (Visual)viewer.ItemContainerGenerator.ContainerFromIndex(index);
        return container.TransformToAncestor(scroll).Transform(new(0, 0)).Y;
    }

    private static string CodeBlock(int index, int lines)
    {
        return "```\n" + string.Join("\n", Enumerable.Range(0, lines).Select(line => $"code {index}.{line}")) + "\n```";
    }
}