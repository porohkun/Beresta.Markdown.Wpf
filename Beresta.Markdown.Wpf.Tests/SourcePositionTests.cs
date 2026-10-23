namespace Beresta.Markdown.Wpf.Tests;

using System.Windows.Controls;
using Presenters;
using Scrolling;

/// <summary>Строки исходника и позиции во вьюере, прокрутка к строке, синхронная прокрутка.</summary>
public sealed class SourcePositionTests
{
    private static readonly string LongText = string.Join("\n\n", Enumerable.Range(0, 2000).Select(i => $"Paragraph {i}"));

    [Fact]
    public void Строка_и_позиция_переводятся_туда_и_обратно()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = LongText };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            foreach (var line in new[] { 0.0, 2, 4.5, 10, 31, 1000, 3500 })
            {
                var offset = viewer.GetSourceLineOffset(line);
                offset.Should().NotBeNull();
                viewer.GetSourceLineAt(offset!.Value).Should().BeApproximately(line, 0.05, $"строка {line}");
            }

            window.Close();
        });
    }

    [Fact]
    public void Позиция_растёт_вместе_со_строкой()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = LongText };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            var offsets = Enumerable.Range(0, 400).Select(i => viewer.GetSourceLineOffset(i * 10)!.Value).ToList();
            offsets.Should().BeInAscendingOrder();
            window.Close();
        });
    }

    [Fact]
    public void Строка_вида_оповещения_стоит_над_его_текстом()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "Intro\n\n> [!WARNING]\n> A callout body" };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            var title = viewer.GetSourceLineOffset(2)!.Value;
            var body = viewer.GetSourceLineOffset(3)!.Value;

            body.Should().BeGreaterThan(title + 10);
            Math.Floor(viewer.GetSourceLineAt((title + body) / 2)!.Value).Should().Be(2);
            window.Close();
        });
    }

    [Fact]
    public void Прокрутка_к_далёкой_строке_ставит_её_в_точку_привязки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = LongText };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            viewer.ScrollToSourceLine(3000, 0.25).Should().BeTrue();
            WpfHost.Settle(window);

            viewer.GetVisibleSourceLine(0.25).Should().BeApproximately(3000, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Строка_кода_встаёт_точно()
    {
        WpfHost.Invoke(() =>
        {
            var code = string.Join("\n", Enumerable.Range(0, 60).Select(i => $"line {i}"));
            var paragraphs = string.Join("\n\n", Enumerable.Range(0, 50).Select(i => $"Paragraph {i}"));
            var text = paragraphs + "\n\n```\n" + code + "\n```\n\n" + paragraphs;
            var viewer = new MarkdownViewer { Markdown = text };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var firstCodeLine = 50 * 2 + 1;

            viewer.ScrollToSourceLine(firstCodeLine + 40).Should().BeTrue();
            WpfHost.Settle(window);

            var block = Visuals.Descendants<MdTextBlock>(viewer).Single(text =>
                text.Runs?.Count() == 1 && viewer.TextMap.TryGetRange(text.Runs!, out var range) && viewer.TextMap.GetText(range).StartsWith("line 0"));
            var lineOffset = code.IndexOf("line 40", StringComparison.Ordinal);
            var rect = block.GetRangeRects(lineOffset, lineOffset + 1).First();
            block.TranslatePoint(rect.TopLeft, scroll).Y.Should().BeApproximately(0, 2);
            window.Close();
        });
    }

    [Fact]
    public void Прокрутка_сообщает_о_смене_видимого()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = LongText };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var raised = 0;
            viewer.ViewportChanged += (_, _) => raised++;

            scroll.ScrollToVerticalOffset(500);
            WpfHost.Settle(window);

            raised.Should().BePositive();
            window.Close();
        });
    }

    [Fact]
    public void Ведомый_вьюер_догоняет_ведущего_со_сдвигом_строк()
    {
        WpfHost.Invoke(() =>
        {
            var left = new MarkdownViewer { Markdown = LongText };
            var right = new MarkdownViewer { Markdown = LongText };
            var leftScroll = new ScrollViewer { Content = left };
            var rightScroll = new ScrollViewer { Content = right };
            Grid.SetColumn(rightScroll, 1);
            var grid = new Grid { ColumnDefinitions = { new(), new() }, Children = { leftScroll, rightScroll } };
            var window = WpfHost.Show(grid);
            using var sync = new MdScrollSync(left, right, new([(0, 10), (4000, 4010)]));

            leftScroll.ScrollToVerticalOffset(20000);
            WpfHost.Settle(window);

            var leftLine = left.GetVisibleSourceLine(0.38)!.Value;
            right.GetVisibleSourceLine(0.38).Should().BeApproximately(leftLine + 10, 1);
            window.Close();
        });
    }
}