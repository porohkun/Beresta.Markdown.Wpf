namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Presenters;
using Selection;
using ViewModels;

/// <summary>Слои пометок: виды, порядок, оформление темы, что под точкой, следование за текстом, быстрый отбор.</summary>
public sealed class MarkTests
{
    private const string ThreeParagraphs = "first paragraph\n\nsecond **bold** paragraph\n\nthird paragraph";

    [Fact]
    public void Набор_отбирает_только_задетые_пометки_по_началу()
    {
        var marks = Enumerable.Range(0, 1000).Select(i => new MdMark(new MdTextRange(i * 10, 3))).ToList();
        marks.Add(new MdMark(new MdTextRange(4995, 100)));
        var set = new MarkSet(new MdMarkLayer("search") { Marks = marks }, MdTextMap.Empty, (_, _) => null);

        var found = set.Within(new MdTextRange(5001, 20)).Select(mark => mark.Range.Start).ToList();

        found.Should().Equal(4995, 5000, 5010, 5020);
    }

    [Fact]
    public void Набор_отбирает_пометки_строк_по_строкам()
    {
        var layer = new MdMarkLayer("diff")
        {
            Marks = [new MdMark(1, 3), new MdMark(5, 5, MdMarkKind.Bar), new MdMark(8, 20, MdMarkKind.Icon)]
        };
        var set = new MarkSet(layer, MdTextMap.Empty, (_, _) => null);

        set.Within(4, 9).Select(mark => mark.First).Should().Equal(5, 8);
    }

    [Fact]
    public void Набор_отбирает_черты_по_месту_вместе_с_краями_куска()
    {
        var layer = new MdMarkLayer("diff")
        {
            Marks =
            [
                new MdMark(new MdTextRange(5, 0), MdMarkKind.Caret),
                new MdMark(new MdTextRange(10, 0), MdMarkKind.Caret),
                new MdMark(new MdTextRange(15, 3), MdMarkKind.Caret),
                new MdMark(new MdTextRange(7, 0))
            ]
        };
        var set = new MarkSet(layer, MdTextMap.Empty, (_, _) => null);

        set.CaretsWithin(new MdTextRange(5, 5)).Select(mark => mark.Range.Start).Should().Equal(5, 10);
        set.Within(new MdTextRange(0, 20)).Should().BeEmpty();
    }

    [Fact]
    public void Черта_встаёт_на_место_пустого_куска_и_в_начало_непустого()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "first paragraph" };
            var window = WpfHost.Show(viewer);
            var inside = new MdMark(new MdTextRange(6, 0), MdMarkKind.Caret) { Brush = Brushes.Red };
            var atEnd = new MdMark(new MdTextRange(15, 0), MdMarkKind.Caret) { Brush = Brushes.Green };
            var wide = new MdMark(new MdTextRange(0, 5), MdMarkKind.Caret) { Brush = Brushes.Blue };
            var layer = new MdMarkLayer("diff") { Marks = [inside, atEnd, wide] };

            viewer.MarkLayers.Add(layer);
            WpfHost.Settle(window);

            var text = Visuals.Descendants<MdTextBlock>(viewer).Single();
            text.Highlights.Should().Equal(
                (0, 0, MdMarkKind.Caret, (Brush)Brushes.Blue),
                (6, 6, MdMarkKind.Caret, (Brush)Brushes.Red),
                (15, 15, MdMarkKind.Caret, (Brush)Brushes.Green));
            text.GetCaretRect(6)!.Value.Left.Should().BeApproximately(text.GetRangeRects(6, 7).First().Left, 0.5);
            text.GetCaretRect(15)!.Value.Left.Should().BeApproximately(text.GetRangeRects(14, 15).First().Right, 0.5);
            viewer.GetMarksAt(6).Should().Equal((layer, inside));
            viewer.GetMarksAt(7).Should().BeEmpty();
            window.Close();
        });
    }

    [Fact]
    public void Черта_на_месте_пустого_куска_следует_за_текстом()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "first\n\nsecond words" };
            var window = WpfHost.Show(viewer);
            var at = viewer.TextMap.Text.IndexOf("words", StringComparison.Ordinal);
            var layer = new MdMarkLayer("diff")
            {
                FollowsText = true,
                Marks = [new MdMark(new MdTextRange(at, 0), MdMarkKind.Caret)]
            };
            viewer.MarkLayers.Add(layer);

            viewer.Markdown = "zero\n\nfirst\n\nsecond words";

            layer.Marks.Should().ContainSingle()
                .Which.Range.Should().Be(new MdTextRange(viewer.TextMap.Text.IndexOf("words", StringComparison.Ordinal), 0));
            window.Close();
        });
    }

    [Fact]
    public void Подсветка_дожидается_раскладки_текста()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "first paragraph" };
            var window = WpfHost.Show(viewer);
            viewer.MarkLayers.Add(new MdMarkLayer("search") { Marks = [new MdMark(new MdTextRange(0, 5)) { Brush = Brushes.Red }] });
            WpfHost.Settle(window);
            var text = Visuals.Descendants<MdTextBlock>(viewer).Single();
            var adorner = AdornerLayer.GetAdornerLayer(text)!.GetAdorners(text)!.Single();

            text.InvalidateMeasure();
            adorner.InvalidateVisual();
            WpfHost.Settle(window);

            VisualTreeHelper.GetDrawing(adorner).Should().NotBeNull();
            VisualTreeHelper.GetDrawing(adorner)!.Bounds.IsEmpty.Should().BeFalse();
            window.Close();
        });
    }

    [Fact]
    public void Слои_рисуются_по_порядку_над_декорациями_а_выделение_поверх_всех()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);

            viewer.Decorations = [new(new(0, 5), Brushes.Yellow)];
            viewer.MarkLayers.Add(new MdMarkLayer("first") { Marks = [new MdMark(new MdTextRange(0, 5)) { Brush = Brushes.Red }] });
            viewer.MarkLayers.Add(new MdMarkLayer("second") { Marks = [new MdMark(new MdTextRange(0, 5), MdMarkKind.Underline) { Brush = Brushes.Green }] });
            viewer.Select(2, 10);
            WpfHost.Settle(window);

            var first = Visuals.Descendants<MdTextBlock>(viewer).First();
            first.Highlights.Select(highlight => highlight.Brush).Should().Equal(Brushes.Yellow, Brushes.Red, Brushes.Green, viewer.SelectionBrush!);
            first.Highlights.Select(highlight => highlight.Kind)
                .Should().Equal(MdMarkKind.Fill, MdMarkKind.Fill, MdMarkKind.Underline, MdMarkKind.Fill);
            window.Close();
        });
    }

    [Fact]
    public void Рамка_и_черта_ложатся_на_неделимые_элементы_краем()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "a `code` b\n\n---\n\nc" };
            var window = WpfHost.Show(viewer);
            var rule = viewer.TextMap.Blocks.Single(entry => entry.Block is ThematicBreakVm).Range;
            var code = new MdTextRange(viewer.TextMap.Text.IndexOf("code", StringComparison.Ordinal), 4);

            viewer.MarkLayers.Add(new MdMarkLayer("review")
            {
                Marks =
                [
                    new MdMark(rule, MdMarkKind.Frame) { Brush = Brushes.Red },
                    new MdMark(code, MdMarkKind.Underline) { Brush = Brushes.Green }
                ]
            });
            WpfHost.Settle(window);

            var item = Visuals.Descendants<MdBlockItem>(viewer).Single(candidate => candidate.Content is ThematicBreakVm);
            item.HighlightBorderBrush.Should().Be(Brushes.Red);
            item.HighlightBorderThickness.Top.Should().BeGreaterThan(0);
            item.Highlight.Should().BeNull();

            var presenter = Visuals.Descendants<InlineCodePresenter>(viewer).Single();
            var overlay = ((Grid)VisualTreeHelper.GetParent(presenter)).Children.OfType<Border>().Single();
            overlay.BorderBrush.Should().Be(Brushes.Green);
            overlay.BorderThickness.Bottom.Should().BeGreaterThan(0);
            overlay.BorderThickness.Top.Should().Be(0);
            window.Close();
        });
    }

    [Fact]
    public void Имя_оформления_берёт_кисть_темы_и_следит_за_её_сменой()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            var range = new MdTextRange(0, 5);

            viewer.MarkLayers.Add(new MdMarkLayer("search")
            {
                Marks = [new MdMark(range) { Style = "Found" }, new MdMark(range, MdMarkKind.Underline) { Style = "Found" }]
            });
            WpfHost.Settle(window);

            var first = Visuals.Descendants<MdTextBlock>(viewer).First();
            first.Highlights.Select(highlight => highlight.Brush)
                .Should().Equal((Brush)viewer.FindResource("Md.Mark.Found.Brush"), (Brush)viewer.FindResource("Md.Mark.Found.LineBrush"));

            viewer.Resources["Md.Mark.Found.Brush"] = Brushes.Red;
            WpfHost.Settle(window);

            first.Highlights.Select(highlight => highlight.Brush).First().Should().Be(Brushes.Red);
            window.Close();
        });
    }

    [Fact]
    public void Строки_во_всю_ширину_ложатся_на_строки_кода()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "intro\n\n```\na\nb\nc\nd\n```" };
            var window = WpfHost.Show(viewer);

            viewer.MarkLayers.Add(new MdMarkLayer("diff") { Marks = [new MdMark(4, 5) { Brush = Brushes.Green }] });
            WpfHost.Settle(window);

            var rows = viewer.RowMarks!.CollectRows();
            rows.Should().ContainSingle();
            var (row, brush) = rows[0];
            brush.Should().Be(Brushes.Green);
            row.Width.Should().Be(viewer.RowMarks.ActualWidth);

            var top = viewer.TranslatePoint(new(0, viewer.GetSourceLineOffset(4)!.Value), viewer.RowMarks).Y;
            var bottom = viewer.TranslatePoint(new(0, viewer.GetSourceLineOffset(6)!.Value), viewer.RowMarks).Y;
            row.Top.Should().BeApproximately(top, 0.5);
            row.Height.Should().BeApproximately(bottom - top, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Строки_во_всю_ширину_ложатся_на_строки_таблицы_от_рамки_до_рамки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "intro\n\n| a | b |\n|---|---|\n| 1 | 2 |\n| 3 | 4 |\n| 5 | 6 |\n\nafter" };
            var window = WpfHost.Show(viewer);

            viewer.MarkLayers.Add(new MdMarkLayer("diff")
            {
                Marks =
                [
                    new MdMark(2, 2) { Brush = Brushes.Green },
                    new MdMark(5, 5) { Brush = Brushes.Blue },
                    new MdMark(6, 6) { Brush = Brushes.Red }
                ]
            });
            WpfHost.Settle(window);

            var marks = viewer.RowMarks!;
            var table = Visuals.Descendants<MdTablePresenter>(viewer).Single();
            var frame = BoundsIn(Visuals.Descendants<Border>(table).Single(border => border.Child is Grid { Name: "PART_Grid" }), marks);
            var cells = Visuals.Descendants<ContentPresenter>(table)
                .Where(presenter => presenter.Content is TableCellVm)
                .Select(presenter => BoundsIn(presenter, marks))
                .ToList();
            var rows = marks.CollectRows().Select(row => row.Row).ToList();

            rows.Should().HaveCount(3);
            rows[0].Top.Should().BeApproximately(frame.Top, 0.5);
            rows[0].Bottom.Should().BeApproximately(cells[0].Bottom, 0.5);
            rows[1].Top.Should().BeApproximately(cells[4].Top, 0.5);
            rows[1].Bottom.Should().BeApproximately(cells[4].Bottom, 0.5);
            rows[2].Top.Should().BeApproximately(cells[6].Top, 0.5);
            rows[2].Bottom.Should().BeApproximately(frame.Bottom, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void У_строки_под_шапкой_таблицы_нет_своего_места()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "| a | b |\n|---|---|\n| 1 | 2 |" };
            var window = WpfHost.Show(viewer);

            viewer.MarkLayers.Add(new MdMarkLayer("diff") { Marks = [new MdMark(1, 1) { Brush = Brushes.Green }] });
            WpfHost.Settle(window);

            viewer.RowMarks!.CollectRows().Should().BeEmpty();
            window.Close();
        });
    }

    [Fact]
    public void Значок_на_полях_встаёт_вровень_с_номером_строки_таблицы()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "| a | b |\n|---|---|\n| 1 | 2 |", ShowSourceLineNumbers = true };
            var window = WpfHost.Show(viewer);

            viewer.MarkLayers.Add(new MdMarkLayer("comments")
            {
                Marks = [new MdMark(2, 2, MdMarkKind.Icon) { Brush = Brushes.Red, Icon = Geometry.Parse("M0,0 L10,0 10,10 Z") }]
            });
            WpfHost.Settle(window);

            var gutter = viewer.Gutter!;
            var icon = gutter.CollectMarks().Single(mark => mark.Kind == MdMarkKind.Icon).Area;
            var number = gutter.CollectNumbers().Single(row => row.Line == 2);
            (icon.Top + icon.Height / 2).Should().BeApproximately(number.Top + number.Height / 2, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Полосы_и_значки_встают_на_поля_и_расширяют_их()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "first\n\nsecond\n\nthird", ShowSourceLineNumbers = true };
            var window = WpfHost.Show(viewer);
            var gutter = viewer.Gutter!;
            var plain = gutter.ActualWidth;

            viewer.MarkLayers.Add(new MdMarkLayer("diff")
            {
                Marks =
                [
                    new MdMark(0, 2, MdMarkKind.Bar) { Brush = Brushes.Green },
                    new MdMark(4, 4, MdMarkKind.Icon) { Brush = Brushes.Red, Icon = Geometry.Parse("M0,0 L10,0 10,10 Z") }
                ]
            });
            WpfHost.Settle(window);

            gutter.ActualWidth.Should().BeApproximately(plain + gutter.BarWidth + gutter.IconSize, 0.5);
            var marks = gutter.CollectMarks();
            var bar = marks.Single(mark => mark.Kind == MdMarkKind.Bar);
            bar.Area.Right.Should().BeApproximately(gutter.ActualWidth, 0.5);
            bar.Area.Height.Should().BeGreaterThan(0);
            var icon = marks.Single(mark => mark.Kind == MdMarkKind.Icon);
            icon.Area.Left.Should().Be(0);
            icon.Area.Width.Should().BeGreaterThan(0);
            window.Close();
        });
    }

    [Fact]
    public void Под_точкой_и_позицией_находятся_пометка_текста_и_строка()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            var second = viewer.TextMap.Text.IndexOf("second", StringComparison.Ordinal);
            var word = new MdMark(new MdTextRange(second, 6)) { Brush = Brushes.Yellow, Tag = "word" };
            var row = new MdMark(2, 2) { Brush = Brushes.Green, Tag = "row" };
            var layer = new MdMarkLayer("notes") { Marks = [word, row] };
            viewer.MarkLayers.Add(layer);
            WpfHost.Settle(window);

            viewer.GetMarksAt(Visuals.PointOf(viewer, second + 2)).Should().Equal((layer, word), (layer, row));
            viewer.GetMarksAt(second + 2).Should().Equal((layer, word), (layer, row));
            viewer.GetMarksAt(Visuals.PointOf(viewer, 2)).Should().BeEmpty();
            window.Close();
        });
    }

    [Fact]
    public void Слой_за_текстом_переносит_пометки_и_убирает_пропавшие()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "first\n\nsecond\n\nthird words" };
            var window = WpfHost.Show(viewer);
            var text = viewer.TextMap.Text;
            var following = new MdMarkLayer("comments")
            {
                FollowsText = true,
                Marks =
                [
                    new MdMark(new MdTextRange(text.IndexOf("second", StringComparison.Ordinal), 6)) { Tag = "gone" },
                    new MdMark(new MdTextRange(text.IndexOf("words", StringComparison.Ordinal), 5)) { Tag = "words" },
                    new MdMark(4, 4) { Tag = "line" }
                ]
            };
            var still = new MdMarkLayer("search") { Marks = [new MdMark(new MdTextRange(0, 5))] };
            viewer.MarkLayers.Add(following);
            viewer.MarkLayers.Add(still);
            var changes = 0;
            following.Changed += (_, _) => changes++;

            viewer.Markdown = "zero\n\nfirst\n\nthird words";

            changes.Should().Be(1);
            following.Marks.Select(mark => mark.Tag).Should().Equal("words", "line");
            viewer.TextMap.GetText(following.Marks[0].Range!.Value).Should().Be("words");
            following.Marks[1].Lines.Should().Be((4, 4));
            still.Marks.Single().Range.Should().Be(new MdTextRange(0, 5));
            window.Close();
        });
    }

    /// <summary>Рамка элемента в координатах другого.</summary>
    private static Rect BoundsIn(FrameworkElement element, Visual target)
        => element.TransformToVisual(target).TransformBounds(new Rect(element.RenderSize));
}
