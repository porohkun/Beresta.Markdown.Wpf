namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Presenters;

/// <summary>Номера строк исходника на полях и номера строк внутри блоков кода.</summary>
public sealed class LineNumberTests
{
    private const string Document =
        "# Title\n" + // 0
        "\n" +
        "First paragraph\n" + // 2
        "second line\n" + // 3
        "\n" +
        "- one\n" + // 5
        "- two\n" + // 6
        "\n" +
        "```\n" + // 8
        "code 0\n" + // 9
        "code 1\n" + // 10
        "code 2\n" + // 11
        "```\n" + // 12
        "\n" +
        "> quoted\n"; // 14

    private static readonly string LongText = string.Join("\n\n", Enumerable.Range(0, 2000).Select(i => $"Paragraph {i}"));

    [Fact]
    public void Поля_нумеруют_каждую_строку_у_которой_есть_место_на_экране()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = Document, ShowSourceLineNumbers = true };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            var numbers = viewer.Gutter!.CollectNumbers();

            numbers.Select(number => number.Line).Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 14);
            numbers.Select(number => number.Top).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
            ShouldNotOverlap(numbers);
            window.Close();
        });
    }

    [Theory]
    [InlineData("a\n\n---\n\nb", new[] { 0, 1, 2, 3, 4 })]
    [InlineData("<section>\nhello\n</section>", new[] { 0, 1, 2 })]
    public void Строка_со_своим_местом_на_экране_получает_номер(string markdown, int[] expected)
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = markdown, ShowSourceLineNumbers = true };
            var window = WpfHost.Show(viewer);

            viewer.Gutter!.CollectNumbers().Select(number => number.Line).Should().Equal(expected);
            window.Close();
        });
    }

    [Theory]
    [InlineData("| a | b |\n|---|---|\n| 1 | 2 |", new[] { 0, 2 })]
    [InlineData("Title\n=====\n\ntext", new[] { 0, 2, 3 })]
    public void Строка_без_своего_места_на_экране_остаётся_без_номера(string markdown, int[] expected)
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = markdown, ShowSourceLineNumbers = true };
            var window = WpfHost.Show(viewer);

            viewer.Gutter!.CollectNumbers().Select(number => number.Line).Should().Equal(expected);
            window.Close();
        });
    }

    [Fact]
    public void Номер_стоит_напротив_своей_строки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = Document, ShowSourceLineNumbers = true };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });
            var gutter = viewer.Gutter!;
            var numbers = gutter.CollectNumbers().ToDictionary(number => number.Line, number => number.Top);
            var presenter = Visuals.Descendants<CodeBlockPresenter>(viewer).Single();
            var header = (FrameworkElement)presenter.Template.FindName("PART_Header", presenter);

            TopOf(TextStartingWith(viewer, "First"), 0, gutter).Should().BeApproximately(numbers[2], 0.5);
            TopOf(TextStartingWith(viewer, "First"), "First paragraph\n".Length, gutter).Should().BeApproximately(numbers[3], 0.5);
            TopOf(TextStartingWith(viewer, "two"), 0, gutter).Should().BeApproximately(numbers[6], 0.5);
            header.TranslatePoint(default, gutter).Y.Should().BeApproximately(numbers[8], 0.5);
            TopOf(TextStartingWith(viewer, "code 0"), "code 0\n".Length, gutter).Should().BeApproximately(numbers[10], 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Поля_перерисовываются_только_когда_блоки_встают_иначе()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = LongText, ShowSourceLineNumbers = true };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var gutter = viewer.Gutter!;
            var renders = gutter.Renders;

            viewer.BlockPanel!.InvalidateMeasure();
            viewer.BlockPanel.InvalidateArrange();
            WpfHost.Settle(window);
            gutter.Renders.Should().Be(renders, "блоки остались на своих местах");

            scroll.ScrollToVerticalOffset(3000);
            WpfHost.Settle(window);
            gutter.Renders.Should().BeGreaterThan(renders, "созданы другие блоки");
            window.Close();
        });
    }

    [Fact]
    public void Поля_и_номера_в_коде_выключены_по_умолчанию()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = Document };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            viewer.Gutter!.Visibility.Should().Be(Visibility.Collapsed);
            viewer.Gutter.ActualWidth.Should().Be(0);
            CodeNumbersOf(viewer).Visibility.Should().Be(Visibility.Collapsed);
            window.Close();
        });
    }

    [Fact]
    public void Номера_в_коде_включаются_отдельно_от_полей()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = Document, ShowCodeLineNumbers = true };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });
            var numbers = CodeNumbersOf(viewer);
            var code = TextStartingWith(viewer, "code 0");

            viewer.Gutter!.Visibility.Should().Be(Visibility.Collapsed);
            numbers.Visibility.Should().Be(Visibility.Visible);
            string.Concat(numbers.Inlines.OfType<Run>().Select(run => run.Text)).Should().Be("1\n2\n3");
            numbers.TranslatePoint(default, code).Y.Should().BeApproximately(0, 0.5);
            numbers.ActualHeight.Should().BeApproximately(code.ActualHeight, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Ширина_полей_по_числу_цифр_последней_строки_и_не_меняется_при_прокрутке()
    {
        WpfHost.Invoke(() =>
        {
            var small = new MarkdownViewer { Markdown = "one\n\ntwo", ShowSourceLineNumbers = true };
            var large = new MarkdownViewer { Markdown = LongText, ShowSourceLineNumbers = true };
            var largeScroll = new ScrollViewer { Content = large };
            Grid.SetColumn(largeScroll, 1);
            var grid = new Grid { ColumnDefinitions = { new(), new() }, Children = { small, largeScroll } };
            var window = WpfHost.Show(grid);

            var width = large.Gutter!.ActualWidth;
            width.Should().BeGreaterThan(small.Gutter!.ActualWidth);

            largeScroll.ScrollToEnd();
            WpfHost.Settle(window);

            large.Gutter.ActualWidth.Should().Be(width);
            window.Close();
        });
    }

    [Fact]
    public void Поля_рисуют_номера_видимых_блоков_и_следуют_за_прокруткой()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = LongText, ShowSourceLineNumbers = true };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });
            var gutter = viewer.Gutter!;

            gutter.CollectNumbers().Count.Should().BeInRange(1, 100);

            viewer.ScrollToSourceLine(2000, 0.5).Should().BeTrue();
            WpfHost.Settle(window);

            var numbers = gutter.CollectNumbers();
            numbers.Select(number => number.Line).Should().Contain(2000);
            ShouldBeDrawn(gutter, numbers);
            window.Close();
        });
    }

    [Fact]
    public void Поля_следуют_за_вложенным_списком()
    {
        WpfHost.Invoke(() =>
        {
            var list = string.Join("\n", Enumerable.Repeat("- item", 500));
            var viewer = new MarkdownViewer { Markdown = list, ShowSourceLineNumbers = true };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var gutter = viewer.Gutter!;

            scroll.ScrollToVerticalOffset(5000);
            WpfHost.Settle(window);

            var numbers = gutter.CollectNumbers();
            numbers.Should().Contain(number => number.Line > 200);
            ShouldNotOverlap(numbers);
            ShouldBeDrawn(gutter, numbers);
            window.Close();
        });
    }

    [Fact]
    public void Номер_контейнера_стоит_у_его_рамки_и_уступает_тексту_вплотную()
    {
        WpfHost.Invoke(() =>
        {
            const string text = "Intro\n\n:::note T\nInside\n:::\n";
            var note = TestWidgets.Note();
            var padded = new LambdaExtension
            {
                Block = note.Block,
                Template = item => item is TestWidgets.NoteVm
                    ? TestWidgets.Template("<Border Tag='note' Padding='0,20,0,0'><p:MdBlocksPresenter Blocks='{Binding Blocks}' /></Border>")
                    : null
            };
            var tightViewer = new MarkdownViewer { Extensions = { note }, Markdown = text, ShowSourceLineNumbers = true };
            var paddedViewer = new MarkdownViewer { Extensions = { padded }, Markdown = text, ShowSourceLineNumbers = true };
            var window = WpfHost.Show(new StackPanel { Children = { tightViewer, paddedViewer } });

            tightViewer.Gutter!.CollectNumbers().Select(number => number.Line).Should().Equal(0, 1, 3);

            var numbers = paddedViewer.Gutter!.CollectNumbers();
            numbers.Select(number => number.Line).Should().Equal(0, 1, 2, 3);
            var border = Visuals.Descendants<Border>(paddedViewer).Single(element => Equals(element.Tag, "note"));
            numbers[2].Top.Should().BeApproximately(border.TranslatePoint(default, paddedViewer.Gutter).Y, 0.5);
            ShouldNotOverlap(numbers);
            window.Close();
        });
    }

    private static void ShouldNotOverlap(IReadOnlyList<(int Line, double Top, double Height)> numbers)
    {
        for (var i = 1; i < numbers.Count; i++)
        {
            numbers[i].Top.Should().BeGreaterThanOrEqualTo(numbers[i - 1].Top + numbers[i - 1].Height - 0.5,
                $"номера строк {numbers[i - 1].Line} и {numbers[i].Line}");
        }
    }

    private static void ShouldBeDrawn(MdLineGutter gutter, IReadOnlyList<(int Line, double Top, double Height)> numbers)
    {
        var drawn = Drawn(gutter);
        drawn.Should().HaveCount(numbers.Count);

        foreach (var (line, top, height) in numbers)
        {
            drawn.Should().Contain(glyphs => glyphs.Top + glyphs.Height / 2 > top && glyphs.Top + glyphs.Height / 2 < top + height, $"номер строки {line}");
        }
    }

    private static List<Rect> Drawn(Visual visual)
    {
        var bounds = new List<Rect>();
        Collect(VisualTreeHelper.GetDrawing(visual));
        return bounds;

        void Collect(Drawing? drawing)
        {
            switch (drawing)
            {
                case DrawingGroup group:
                    foreach (var child in group.Children)
                    {
                        Collect(child);
                    }

                    break;
                case GlyphRunDrawing glyphs:
                    bounds.Add(glyphs.Bounds);
                    break;
            }
        }
    }

    private static MdTextBlock TextStartingWith(MarkdownViewer viewer, string start)
    {
        return Visuals.Descendants<MdTextBlock>(viewer).First(text =>
            text.Runs is { } runs && viewer.TextMap.TryGetRange(runs, out var range) &&
            viewer.TextMap.GetText(range).StartsWith(start, StringComparison.Ordinal));
    }

    private static double TopOf(MdTextBlock text, int offset, UIElement target)
        => text.TranslatePoint(text.GetRangeRects(offset, offset + 1).First().TopLeft, target).Y;

    private static TextBlock CodeNumbersOf(MarkdownViewer viewer)
    {
        var presenter = Visuals.Descendants<CodeBlockPresenter>(viewer).Single();
        return (TextBlock)presenter.Template.FindName("LineNumbers", presenter);
    }
}