namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Controls;
using Presenters;
using Scrolling;
using ViewModels;

/// <summary>Пустые места: куда встают по модели и сколько места занимают на экране.</summary>
public sealed class GapTests
{
    [Fact]
    public void Место_между_блоками_встаёт_перед_блоком_своей_строки()
    {
        var blocks = ModelBuilder.Parse("a\n\nb\n\nc");
        var layout = new GapLayout(blocks, [new(2, 10), new(2, 5)]);

        layout.Before(blocks.OfType<ParagraphVm>().ElementAt(1)).Should().Be(15);
    }

    [Fact]
    public void Место_у_строки_пункта_встаёт_перед_пунктом_и_входит_в_список()
    {
        var blocks = ModelBuilder.Parse("- one\n- two\n- three");
        var list = blocks.OfType<ListVm>().Single();
        var layout = new GapLayout(blocks, [new(1, 20)]);

        layout.Before(list.Items[1]).Should().Be(20);
        layout.Before(list.Items[1].Blocks[0]).Should().Be(0);
        layout.Inside(list).Should().Be(20);
    }

    [Fact]
    public void Место_у_первой_строки_списка_встаёт_перед_всем_списком()
    {
        var blocks = ModelBuilder.Parse("intro\n\n- one\n- two");
        var list = blocks.OfType<ListVm>().Single();
        var layout = new GapLayout(blocks, [new(2, 20)]);

        layout.Before(list).Should().Be(20);
        layout.Before(list.Items[0]).Should().Be(0);
    }

    [Fact]
    public void Место_у_строки_абзаца_встаёт_в_его_текст()
    {
        var blocks = ModelBuilder.Parse("first\nsecond");
        var paragraph = blocks.OfType<ParagraphVm>().Single();
        var layout = new GapLayout(blocks, [new(1, 30)]);

        layout.InText(paragraph.Inlines).Should().Equal((6, 30.0));
        layout.Inside(paragraph).Should().Be(30);
    }

    [Fact]
    public void Место_у_строки_кода_встаёт_в_код_и_в_номера()
    {
        var blocks = ModelBuilder.Parse("```\na\nb\n```");
        var code = blocks.OfType<CodeBlockVm>().Single();
        var layout = new GapLayout(blocks, [new(2, 12)]);

        layout.InText(code.Runs).Should().Equal((2, 12.0));
        layout.InCode(code).Should().Equal((1, 12.0));
    }

    [Fact]
    public void Место_у_строки_таблицы_встаёт_перед_строкой_а_у_разделителя_перед_таблицей()
    {
        var blocks = ModelBuilder.Parse("| a |\n|---|\n| 1 |\n| 2 |");
        var table = blocks.OfType<TableVm>().Single();

        new GapLayout(blocks, [new(3, 8)]).Before(table.Rows[2]).Should().Be(8);
        new GapLayout(blocks, [new(1, 8)]).Before(table).Should().Be(8);
    }

    [Fact]
    public void Место_внутри_цитаты_встаёт_перед_её_блоком()
    {
        var blocks = ModelBuilder.Parse("> a\n>\n> b");
        var quote = blocks.OfType<QuoteVm>().Single();
        var layout = new GapLayout(blocks, [new(2, 9)]);

        layout.Before(quote.Blocks.OfType<ParagraphVm>().Last()).Should().Be(9);
        layout.Inside(quote).Should().Be(9);
    }

    [Fact]
    public void Место_за_концом_документа_встаёт_после_последнего_блока()
    {
        var blocks = ModelBuilder.Parse("a\n\nb");
        var layout = new GapLayout(blocks, [new(10, 7)]);

        layout.After(blocks[^1]).Should().Be(7);
    }

    [Theory]
    [InlineData("a\n\nb", 2)]
    [InlineData("first\nsecond", 1)]
    [InlineData("```\na\nb\nc\n```", 2)]
    [InlineData("| a |\n|---|\n| 1 |\n| 2 |", 3)]
    [InlineData("- one\n- two", 1)]
    [InlineData("> a\n>\n> b", 2)]
    public void Место_отодвигает_свою_строку_на_свою_высоту(string markdown, int line)
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = markdown, ShowCodeLineNumbers = true };
            var window = WpfHost.Show(viewer);
            var before = viewer.GetSourceLineOffset(line)!.Value;
            var previous = viewer.GetSourceLineOffset(line - 1)!.Value;

            viewer.Gaps = [new(line, 40)];
            WpfHost.Settle(window);

            viewer.GetSourceLineOffset(line)!.Value.Should().BeApproximately(before + 40, 0.5);
            viewer.GetSourceLineOffset(line - 1)!.Value.Should().BeApproximately(previous, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Номера_строк_кода_отодвигаются_вместе_с_кодом()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "```\na\nb\nc\n```", ShowCodeLineNumbers = true };
            var window = WpfHost.Show(viewer);

            viewer.Gaps = [new(3, 25)];
            WpfHost.Settle(window);

            var presenter = Visuals.Descendants<CodeBlockPresenter>(viewer).Single();
            var numbers = (TextBlock)presenter.Template.FindName("LineNumbers", presenter);
            var code = Visuals.Descendants<MdTextBlock>(presenter).Single();
            numbers.ActualHeight.Should().BeApproximately(code.ActualHeight, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Место_не_текст_и_не_меняет_выделения()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "first\nsecond\n\n```\na\nb\n```" };
            var window = WpfHost.Show(viewer);

            viewer.Gaps = [new(1, 20), new(5, 20)];
            WpfHost.Settle(window);
            viewer.SelectAll();

            viewer.SelectedText.Should().Be("first\nsecond\n\na\nb");
            Visuals.Descendants<MdTextBlock>(viewer).SelectMany(text => text.GapRects).Should().HaveCount(2);
            window.Close();
        });
    }

    [Fact]
    public void Место_внутри_несозданного_блока_входит_в_высоту_документа()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n\n", Enumerable.Range(0, 300).Select(i => $"Line {i} first\nLine {i} second"));
            var viewer = new MarkdownViewer { Markdown = text };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var height = scroll.ExtentHeight;

            viewer.Gaps = [new(3 * 250 + 1, 500)];
            WpfHost.Settle(window);

            scroll.ExtentHeight.Should().BeApproximately(height + 500, 1);
            window.Close();
        });
    }
}
