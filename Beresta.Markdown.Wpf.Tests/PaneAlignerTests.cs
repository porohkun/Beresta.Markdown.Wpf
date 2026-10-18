namespace Beresta.Markdown.Wpf.Tests;

using System.Windows.Controls;
using Scrolling;

/// <summary>Две панели вровень и замена карты у помощников двух панелей.</summary>
public sealed class PaneAlignerTests
{
    private const string Left = "first\n\nsecond\n\nthird";
    private const string Right = "first\n\nadded\nparagraph\n\nsecond\n\nthird";

    /// <summary>Совпавшие строки: first, second, third; справа вставлены две строки и пустая.</summary>
    private static readonly MdLineMap Diff = new([(0, 0), (1, 1), (2, 5), (3, 6), (4, 7), (5, 8)]);

    [Fact]
    public void Совпавшие_строки_встают_вровень_а_напротив_вставки_пустое_место()
    {
        WpfHost.Invoke(() =>
        {
            var (left, right, window) = ShowInOneScroll(Left, Right);
            using var aligner = new MdPaneAligner(left, right, Diff);

            aligner.Align();
            WpfHost.Settle(window);

            left.Gaps.Should().ContainSingle().Which.Line.Should().Be(2);
            right.Gaps.Should().BeNullOrEmpty();
            left.GetSourceLineOffset(2).Should().BeApproximately(right.GetSourceLineOffset(5)!.Value, 0.5);
            left.GetSourceLineOffset(4).Should().BeApproximately(right.GetSourceLineOffset(7)!.Value, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Удалённый_абзац_перед_заголовком_даёт_место_только_напротив()
    {
        WpfHost.Invoke(() =>
        {
            var (left, right, window) = ShowInOneScroll("a\n\nremoved\n\n# Heading\n\nb", "a\n\n# Heading\n\nb");
            using var aligner = new MdPaneAligner(left, right, new([(0, 0), (1, 1), (2, 2), (4, 2), (5, 3), (6, 4), (7, 5)]));

            WpfHost.Settle(window);

            left.Gaps.Should().BeNullOrEmpty();
            right.Gaps.Should().ContainSingle().Which.Line.Should().Be(2);
            left.GetSourceLineOffset(4).Should().BeApproximately(right.GetSourceLineOffset(2)!.Value, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Удалённая_последняя_строка_кода_даёт_место_в_коде_напротив()
    {
        WpfHost.Invoke(() =>
        {
            var (left, right, window) = ShowInOneScroll("```\none\ntwo\nthree\n```\n\nafter", "```\none\ntwo\n```\n\nafter");
            using var aligner = new MdPaneAligner(left, right, new([(0, 0), (1, 1), (2, 2), (3, 3), (4, 3), (5, 4), (6, 5), (7, 6)]));
            WpfHost.Settle(window);
            var gaps = right.Gaps;

            aligner.Align();
            WpfHost.Settle(window);

            left.Gaps.Should().BeNullOrEmpty();
            right.Gaps.Should().ContainSingle().Which.Line.Should().Be(3);
            right.Gaps.Should().BeEquivalentTo(gaps, "места не растут");
            right.GetSourceLineOffset(3).Should().BeApproximately(left.GetSourceLineOffset(4)!.Value, 0.5, "место внутри кода");
            left.GetSourceLineOffset(6).Should().BeApproximately(right.GetSourceLineOffset(5)!.Value, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Выравниватель_сам_расставляет_места_после_раскладки()
    {
        WpfHost.Invoke(() =>
        {
            var (left, right, window) = ShowInOneScroll(Left, Right);
            using var aligner = new MdPaneAligner(left, right, Diff);

            WpfHost.Settle(window);

            left.GetSourceLineOffset(2).Should().BeApproximately(right.GetSourceLineOffset(5)!.Value, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Новая_карта_расставляет_места_заново_а_закрытие_их_убирает()
    {
        WpfHost.Invoke(() =>
        {
            var (left, right, window) = ShowInOneScroll(Left, Left);
            var aligner = new MdPaneAligner(left, right, MdLineMap.Identity);
            aligner.Align();
            left.Gaps.Should().BeNullOrEmpty();
            right.Gaps.Should().BeNullOrEmpty();

            aligner.Map = new([(0, 0), (2, 4)]);
            aligner.Align();
            WpfHost.Settle(window);
            left.Gaps.Should().ContainSingle().Which.Line.Should().Be(2);

            aligner.Dispose();
            left.Gaps.Should().BeNull();
            right.Gaps.Should().BeNull();
            window.Close();
        });
    }

    [Fact]
    public void Места_не_растут_при_прокрутке_мимо_несозданных_блоков()
    {
        WpfHost.Invoke(() =>
        {
            var tail = string.Join("\n\n", Enumerable.Range(0, 150).Select(i => $"Paragraph {i}"));
            var leftText = "- one\n- two\n- three\n\n" + tail;
            var rightText = "- one\n- added\n- two\n- three\n\n" + tail;
            var lines = leftText.Split('\n').Length;
            var matches = new List<(int Left, int Right)> { (0, 0) };
            matches.AddRange(Enumerable.Range(1, lines - 1).Select(line => (line, line + 1)));
            var anchors = matches.SelectMany(match => new[] { ((double)match.Left, (double)match.Right), (match.Left + 1.0, match.Right + 1.0) });

            var leftViewer = new MarkdownViewer { Markdown = leftText };
            var rightViewer = new MarkdownViewer { Markdown = rightText };
            Grid.SetColumn(rightViewer, 1);
            var grid = new Grid { ColumnDefinitions = { new(), new() }, Children = { leftViewer, rightViewer } };
            var scroll = new ScrollViewer { Content = grid };
            var window = WpfHost.Show(scroll);
            using var aligner = new MdPaneAligner(leftViewer, rightViewer, new(anchors));
            WpfHost.Settle(window);
            var item = rightViewer.GetSourceLineOffset(2)!.Value - rightViewer.GetSourceLineOffset(1)!.Value;

            double RoundTrip()
            {
                scroll.ScrollToVerticalOffset(4000);
                WpfHost.Settle(window);
                scroll.ScrollToVerticalOffset(0);
                WpfHost.Settle(window);
                return (leftViewer.Gaps ?? []).Sum(gap => gap.Height) + (rightViewer.Gaps ?? []).Sum(gap => gap.Height);
            }

            var first = RoundTrip();
            var second = RoundTrip();

            second.Should().BeApproximately(first, 1);
            first.Should().BeLessThan(item * 3);
            leftViewer.GetSourceLineOffset(1).Should().BeApproximately(rightViewer.GetSourceLineOffset(2)!.Value, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Две_отдельные_прокрутки_держат_одно_смещение()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n\n", Enumerable.Range(0, 200).Select(i => $"Paragraph {i}"));
            var left = new MarkdownViewer { Markdown = text };
            var right = new MarkdownViewer { Markdown = text };
            var leftScroll = new ScrollViewer { Content = left };
            var rightScroll = new ScrollViewer { Content = right };
            Grid.SetColumn(rightScroll, 1);
            var grid = new Grid { ColumnDefinitions = { new(), new() }, Children = { leftScroll, rightScroll } };
            var window = WpfHost.Show(grid);
            using var aligner = new MdPaneAligner(left, right, MdLineMap.Identity);
            WpfHost.Settle(window);

            leftScroll.ScrollToVerticalOffset(1200);
            WpfHost.Settle(window);

            rightScroll.VerticalOffset.Should().BeApproximately(leftScroll.VerticalOffset, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Новая_карта_синхронной_прокрутки_сразу_подтягивает_второй_вьюер()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n\n", Enumerable.Range(0, 200).Select(i => $"Paragraph {i}"));
            var left = new MarkdownViewer { Markdown = text };
            var right = new MarkdownViewer { Markdown = text };
            var leftScroll = new ScrollViewer { Content = left };
            var rightScroll = new ScrollViewer { Content = right };
            Grid.SetColumn(rightScroll, 1);
            var grid = new Grid { ColumnDefinitions = { new(), new() }, Children = { leftScroll, rightScroll } };
            var window = WpfHost.Show(grid);
            using var sync = new MdScrollSync(left, right, MdLineMap.Identity);
            leftScroll.ScrollToVerticalOffset(1200);
            WpfHost.Settle(window);

            sync.Map = new([(0, 20), (1000, 1020)]);
            WpfHost.Settle(window);

            var leftLine = left.GetVisibleSourceLine(0.38)!.Value;
            right.GetVisibleSourceLine(0.38).Should().BeApproximately(leftLine + 20, 1);
            window.Close();
        });
    }

    private static (MarkdownViewer Left, MarkdownViewer Right, System.Windows.Window Window) ShowInOneScroll(string left, string right)
    {
        var leftViewer = new MarkdownViewer { Markdown = left };
        var rightViewer = new MarkdownViewer { Markdown = right };
        Grid.SetColumn(rightViewer, 1);
        var grid = new Grid { ColumnDefinitions = { new(), new() }, Children = { leftViewer, rightViewer } };
        var window = WpfHost.Show(new ScrollViewer { Content = grid });
        return (leftViewer, rightViewer, window);
    }
}
