namespace Beresta.Markdown.Wpf.Tests;

using System.Windows.Controls;
using Presenters;
using Selection;
using ViewModels;

/// <summary>Таблицы: ячейки-добивки, шапка, объединённые ячейки сетки, перенос текста по ширине вьюера.</summary>
public sealed class TableTests
{
    private const string ShortRows = "| A | B | C |\n|---|---|---|\n| 1 |\n| 2 |";

    private const string SpannedGrid =
        "+---+---+---+\n| a | b | c |\n+===+===+===+\n| wide  | x |\n+-------+---+\n| 1 | 2 | 3 |\n+---+---+---+";

    [Fact]
    public void У_каждой_ячейки_добивки_свой_кусок_карты()
    {
        var blocks = ModelBuilder.Parse(ShortRows);
        var map = MdTextMap.Build(blocks, ShortRows);

        var starts = blocks.OfType<TableVm>().Single().Rows
            .SelectMany(row => row.Cells)
            .Select(cell => map.TryGetRange(cell.Inlines, out var range) ? range.Start : -1)
            .ToList();

        starts.Should().NotContain(-1).And.BeInAscendingOrder().And.OnlyHaveUniqueItems();
        map.GetText(map.RangeOfSourceLines(2, 2)!.Value).Should().NotContain("2");
    }

    [Fact]
    public void Сетка_без_двойной_черты_шапки_не_получает()
    {
        var table = ModelBuilder.Parse("+---+---+\n| a | b |\n+---+---+\n| 1 | 2 |\n+---+---+").OfType<TableVm>().Single();

        table.Rows.Select(row => row.IsHeader).Should().Equal(false, false);
    }

    [Fact]
    public void Объединённая_ячейка_сетки_занимает_свои_столбцы()
    {
        var table = ModelBuilder.Parse(SpannedGrid).OfType<TableVm>().Single();

        table.Rows[1].Cells.Select(cell => (ModelDump.Text(cell.Inlines), cell.ColumnSpan)).Should().Equal(("wide", 2), ("x", 1));
        table.Rows.Select(row => row.Cells.Sum(cell => cell.ColumnSpan)).Should().AllBeEquivalentTo(3);
    }

    [Fact]
    public void Объединённая_ячейка_рисуется_через_свои_столбцы()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = SpannedGrid };
            var window = WpfHost.Show(viewer);

            var cells = Visuals.Descendants<ContentPresenter>(viewer).Where(presenter => presenter.Content is TableCellVm).ToList();
            var wide = cells.Single(presenter => ModelDump.Text(((TableCellVm)presenter.Content).Inlines) == "wide");
            var x = cells.Single(presenter => ModelDump.Text(((TableCellVm)presenter.Content).Inlines) == "x");
            (Grid.GetColumn(wide), Grid.GetColumnSpan(wide), Grid.GetColumn(x)).Should().Be((0, 2, 2));
            window.Close();
        });
    }

    [Fact]
    public void Длинный_текст_ячейки_переносится_по_ширине_вьюера()
    {
        WpfHost.Invoke(() =>
        {
            var words = string.Join(" ", Enumerable.Range(0, 120).Select(i => $"word{i}"));
            var viewer = new MarkdownViewer { Markdown = $"| Key | Description |\n|---|---|\n| id | {words} |" };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            var table = Visuals.Descendants<MdTablePresenter>(viewer).Single();
            var scroll = Visuals.Descendants<ScrollViewer>(table).Single();
            scroll.ExtentWidth.Should().BeLessThanOrEqualTo(scroll.ViewportWidth + 0.5);
            table.ActualWidth.Should().BeLessThanOrEqualTo(viewer.ActualWidth + 0.5);

            var texts = Visuals.Descendants<MdTextBlock>(table).ToList();
            var key = texts.Single(text => ModelDump.Text(text.Runs!) == "id");
            var description = texts.Single(text => ModelDump.Text(text.Runs!) == words);
            description.DesiredSize.Height.Should().BeGreaterThan(key.DesiredSize.Height * 3);
            window.Close();
        });
    }

    [Fact]
    public void Короткая_таблица_не_переносит_текст()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "| Key | Value |\n|---|---|\n| one two | three four |" };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            var texts = Visuals.Descendants<MdTextBlock>(Visuals.Descendants<MdTablePresenter>(viewer).Single()).ToList();
            texts.Select(text => text.DesiredSize.Height).Should().AllBeEquivalentTo(texts[0].DesiredSize.Height);
            window.Close();
        });
    }

    [Fact]
    public void Ячейка_из_блоков_по_естественной_ширине_не_переносится_и_при_округлении_раскладки()
    {
        WpfHost.Invoke(() =>
        {
            const string grid = "+------------------+----------------+\n| Cell             | Content        |\n+==================+================+\n"
                + "| First paragraph  | - item         |\n|                  | - another item |\n| Second paragraph |                |\n+------------------+----------------+";
            var viewer = new MarkdownViewer { Markdown = grid };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer, UseLayoutRounding = true });

            var texts = Visuals.Descendants<MdTextBlock>(Visuals.Descendants<MdTablePresenter>(viewer).Single())
                .Where(text => text.IsVisible)
                .ToList();
            texts.Select(text => text.DesiredSize.Height).Should().AllBeEquivalentTo(texts[0].DesiredSize.Height);
            window.Close();
        });
    }

    [Fact]
    public void Слово_длиннее_вьюера_прокручивает_таблицу()
    {
        WpfHost.Invoke(() =>
        {
            var word = new string('w', 300);
            var viewer = new MarkdownViewer { Markdown = $"| A | B |\n|---|---|\n| {word} | short text |" };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            var scroll = Visuals.Descendants<ScrollViewer>(Visuals.Descendants<MdTablePresenter>(viewer).Single()).Single();
            scroll.ExtentWidth.Should().BeGreaterThan(scroll.ViewportWidth);
            window.Close();
        });
    }
}
