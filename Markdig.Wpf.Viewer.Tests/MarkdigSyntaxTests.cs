namespace MarkdigWpfViewer.Tests;

using System.Windows.Documents;
using Markdig;
using Markdig.Extensions.SmartyPants;
using Presenters;
using Selection;
using ViewModels;

/// <summary>
/// Конструкции, которые разбор Markdig узнаёт: индексы, маркер, вставка, название произведения,
/// сокращения, буквенные списки, SmartyPants, сетка-таблица, фигура, подвал.
/// </summary>
public sealed class MarkdigSyntaxTests
{
    private const string GridTable = "+---+---+\n| a | b |\n+===+===+\n| 1 | 2 |\n|   |   |\n| x | y |\n+---+---+";

    [Fact]
    public void Тильда_и_крышка_дают_индексы()
    {
        var runs = Runs(ModelBuilder.Parse("H~2~O и x^3^"));

        runs.Single(run => run.Text == "2").Subscript.Should().BeTrue();
        runs.Single(run => run.Text == "3").Superscript.Should().BeTrue();
        runs.Where(run => run.Text.Contains('H') || run.Text.Contains('x'))
            .Should().NotBeEmpty().And.OnlyContain(run => !run.Subscript && !run.Superscript);
    }

    [Fact]
    public void Маркер_и_вставка_пишутся_разметкой_и_тегами()
    {
        var runs = Runs(ModelBuilder.Parse("==a== ++b++ <mark>c</mark> <ins>d</ins> <u>e</u>"));

        runs.Where(run => run.Marked).Select(run => run.Text).Should().Equal("a", "c");
        runs.Where(run => run.Inserted).Select(run => run.Text).Should().Equal("b", "d", "e");
    }

    [Fact]
    public void Название_произведения_своим_начертанием()
    {
        Runs(ModelBuilder.Parse("\"\"Война и мир\"\" и <cite>Анна</cite>"))
            .Where(run => run.Cite)
            .Select(run => run.Text)
            .Should().Equal("Война и мир", "Анна");
    }

    [Fact]
    public void Сокращение_несёт_расшифровку_а_определение_не_рисуется()
    {
        var blocks = ModelBuilder.Parse("*[HTML]: Hyper Text Markup\n\nHTML here");

        ModelDump.Of(blocks).Should().Be("P(HTML here)");
        Runs(blocks).Single(run => run.Text == "HTML").Abbreviation.Should().Be("Hyper Text Markup");
    }

    [Theory]
    [InlineData("c. three\nd. four", "c.", "d.")]
    [InlineData("iv. four\nv. five", "iv.", "v.")]
    [InlineData("B. bee\nC. sea", "B.", "C.")]
    [InlineData("7) seven\n8) eight", "7)", "8)")]
    public void Маркер_пункта_по_виду_списка(string markdown, string first, string second)
    {
        ModelBuilder.Parse(markdown).OfType<ListVm>().Single().Items
            .Select(item => item.MarkerText)
            .Should().Equal(first, second);
    }

    [Fact]
    public void SmartyPants_из_своей_настройки_разбора_рисует_замены()
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseSmartyPants().Build();

        Text(ModelBuilder.Parse("\"Quotes\" -- dash --- em... and <<this>>", pipeline))
            .Should().Be("“Quotes” – dash — em… and «this»");
        Text(ModelBuilder.Parse("\"Quotes\" -- dash")).Should().Be("\"Quotes\" -- dash");
    }

    [Fact]
    public void SmartyPants_берёт_таблицу_замен_из_настройки()
    {
        var options = new SmartyPantOptions();
        options.Mapping[SmartyPantType.LeftDoubleQuote] = "&laquo;";
        options.Mapping[SmartyPantType.RightDoubleQuote] = "&raquo;";
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseSmartyPants(options).Build();

        Text(ModelBuilder.Parse("\"Цитата\"", pipeline)).Should().Be("«Цитата»");
    }

    [Fact]
    public void Ячейка_сетки_таблицы_из_нескольких_абзацев_держит_их_все()
    {
        var blocks = ModelBuilder.Parse(GridTable);
        var cell = blocks.OfType<TableVm>().Single().Rows[1].Cells[0];

        cell.HasBlocks.Should().BeTrue();
        ModelDump.Of(cell.Blocks).Should().Be("P(1) _ P(x)");
        MdTextMap.Build(blocks).Text.Should().Contain("x").And.Contain("y");
    }

    [Fact]
    public void Пустая_строка_ячейки_даёт_отступ_и_при_тексте_в_соседнем_столбце()
    {
        var table = ModelBuilder.Parse("+-----+-----+\n| a   | b   |\n+=====+=====+\n| one | x   |\n|     | y   |\n| two | z   |\n+-----+-----+")
            .OfType<TableVm>()
            .Single();

        ModelDump.Of(table.Rows[1].Cells[0].Blocks).Should().Be("P(one) _ P(two)");
    }

    [Fact]
    public void Ячейка_из_блоков_рисуется_блоками_и_выделяется()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = GridTable };
            var window = WpfHost.Show(viewer);

            Visuals.Descendants<MdTextBlock>(viewer)
                .Where(text => text.IsVisible)
                .Select(Plain)
                .Should().Contain(["1", "x", "2", "y"]);
            viewer.SelectAll();
            viewer.SelectedText.Should().Contain("x").And.Contain("y");
            window.Close();
        });
    }

    [Fact]
    public void Фигура_держит_содержимое_и_подпись_на_своих_местах()
    {
        ModelDump.Of("^^^\n![img](a.png)\n^^^ Caption\n\n^^^ Top\n![b](b.png)\n^^^")
            .Should().Be("Fig[P(![img]) Cap(Caption)] _ Fig[Cap(Top) P(![b])]");
    }

    [Fact]
    public void Подвал_рисуется_мелким_текстом()
    {
        ModelDump.Of("text\n\n^^ Footer text").Should().Be("P(text) _ Foot[P(Footer text)]");

        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "text\n\n^^ Footer text" };
            var window = WpfHost.Show(viewer);
            var texts = Visuals.Descendants<MdTextBlock>(viewer).ToList();

            texts.Single(text => Plain(text) == "Footer text").FontSize
                .Should().BeLessThan(texts.Single(text => Plain(text) == "text").FontSize);
            window.Close();
        });
    }

    [Fact]
    public void Новые_начертания_рисуются_стилями_темы()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "*[HTML]: Hyper\n\n==a== ++b++ \"\"c\"\" HTML" };
            var window = WpfHost.Show(viewer);
            var text = Visuals.Descendants<MdTextBlock>(viewer).Single();
            var spans = AllInlines(text.Inlines).OfType<Span>().ToList();

            new[] { text.MarkedStyle, text.InsertedStyle, text.CiteStyle, text.AbbreviationStyle }.Should().NotContainNulls();
            spans.Should().Contain(span => span.Style == text.MarkedStyle)
                .And.Contain(span => span.Style == text.InsertedStyle)
                .And.Contain(span => span.Style == text.CiteStyle);
            spans.Single(span => span.Style == text.AbbreviationStyle).ToolTip.Should().Be("Hyper");
            window.Close();
        });
    }

    private static List<RunVm> Runs(IEnumerable<BlockVm> blocks)
        => blocks.OfType<ParagraphVm>().SelectMany(paragraph => paragraph.Inlines).OfType<RunVm>().ToList();

    private static string Text(IEnumerable<BlockVm> blocks)
        => string.Concat(blocks.OfType<ParagraphVm>().Select(paragraph => ModelDump.Text(paragraph.Inlines)));

    private static string Plain(MdTextBlock text)
        => string.Concat((text.Runs ?? []).OfType<RunVm>().Select(run => run.Text));

    private static IEnumerable<Inline> AllInlines(InlineCollection inlines)
        => inlines.SelectMany(inline => inline is Span span ? AllInlines(span.Inlines).Prepend(span) : [inline]);
}
