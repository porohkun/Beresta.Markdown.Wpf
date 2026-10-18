namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using Presenters;
using Scrolling;
using Selection;

/// <summary>Координаты: строки внутри абзаца, ответы в координатах выбранного элемента, событие раскладки.</summary>
public sealed class CoordinatesTests
{
    [Fact]
    public void Строка_абзаца_стоит_на_своей_экранной_строке()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer
            {
                Width = 160,
                Markdown = "first line is long enough to wrap into several screen rows here\nsecond line"
            };
            var window = WpfHost.Show(viewer);
            var text = Visuals.Descendants<MdTextBlock>(viewer).First();
            var row = text.SourceRows.Single(entry => entry.Line == 1).Row;
            var expected = text.TranslatePoint(new(0, row.Top), viewer).Y;

            viewer.GetSourceLineOffset(1).Should().BeApproximately(expected, 0.5);
            Math.Floor(viewer.GetSourceLineAt(expected + 1)!.Value).Should().Be(1);
            Math.Floor(viewer.GetSourceLineAt(expected - 1)!.Value).Should().Be(0);
            window.Close();
        });
    }

    [Fact]
    public void Координаты_отвечают_в_выбранном_элементе()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "first paragraph\n\nsecond paragraph" };
            var grid = new Grid { ColumnDefinitions = { new() { Width = new(50) }, new() } };
            Grid.SetColumn(viewer, 1);
            grid.Children.Add(viewer);
            var window = WpfHost.Show(grid);
            var coordinates = viewer.GetCoordinates(grid);
            var range = new MdTextRange(viewer.TextMap.Text.IndexOf("second", StringComparison.Ordinal), 6);

            var own = viewer.GetRangeRects(range).Single();
            var rect = coordinates.RangeRects(range).Single();
            rect.Left.Should().BeApproximately(own.Left + 50, 0.5);
            rect.Top.Should().BeApproximately(own.Top, 0.5);

            coordinates.LineTop(2).Should().BeApproximately(viewer.GetSourceLineOffset(2)!.Value, 0.5);
            var lines = coordinates.LinesRect(2, 2)!.Value;
            lines.Left.Should().BeApproximately(50, 0.5);
            lines.Width.Should().BeApproximately(viewer.ActualWidth, 0.5);
            lines.Height.Should().BeGreaterThan(0);

            var middle = new Point(rect.Left + 1, rect.Top + rect.Height / 2);
            coordinates.PositionAt(middle, snapToText: false).Should().Be(range.Start);
            Math.Floor(coordinates.LineAt(middle)!.Value).Should().Be(2);
            window.Close();
        });
    }

    [Fact]
    public void Видимые_строки_берутся_у_краёв_прокрутки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = string.Join("\n\n", Enumerable.Range(0, 200).Select(i => $"Paragraph {i}")) };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);

            scroll.ScrollToVerticalOffset(1500);
            WpfHost.Settle(window);

            var visible = viewer.GetCoordinates(scroll).VisibleLines()!.Value;
            visible.First.Should().Be((int)Math.Floor(viewer.GetVisibleSourceLine(0)!.Value));
            visible.Last.Should().Be((int)Math.Floor(viewer.GetVisibleSourceLine(1)!.Value));
            visible.First.Should().BeGreaterThan(0);
            window.Close();
        });
    }

    [Fact]
    public void Видимые_строки_со_своим_местом_не_включают_строк_без_него()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "| a | b |\n|---|---|\n| 1 | 2 |\n\ntext" };
            var window = WpfHost.Show(viewer);

            var rows = viewer.GetCoordinates(viewer).VisibleRows();

            rows.Select(row => row.Line).Should().Equal(0, 2, 3, 4);
            rows.Should().OnlyContain(row => row.Height > 0);
            rows.Single(row => row.Line == 4).Top.Should().BeApproximately(viewer.GetSourceLineOffset(4)!.Value, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Прокрутка_внутри_блока_сообщает_о_смене_раскладки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = $"```\n{new string('x', 400)}\n```" };
            var window = WpfHost.Show(viewer);
            var changes = 0;
            viewer.ViewportChanged += (_, _) => changes++;
            WpfHost.Settle(window);
            changes = 0;

            var inner = Visuals.Descendants<ScrollViewer>(viewer).Single();
            inner.ScrollToHorizontalOffset(100);
            WpfHost.Settle(window);

            changes.Should().BeGreaterThan(0);
            window.Close();
        });
    }
}
