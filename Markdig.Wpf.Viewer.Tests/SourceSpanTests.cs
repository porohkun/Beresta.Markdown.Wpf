namespace MarkdigWpfViewer.Tests;

using ViewModels;

/// <summary>У каждого элемента модели — его позиция в исходнике.</summary>
public sealed class SourceSpanTests
{
    [Fact]
    public void Блок_знает_свои_строки_и_символы()
    {
        const string text = "a\n\nline1\nline2";
        var paragraph = ModelBuilder.Parse(text)[2];

        paragraph.Source.Should().Be(new MdSourceSpan(3, 11, 2, 3));
        Slice(text, paragraph.Source).Should().Be("line1\nline2");
    }

    [Fact]
    public void Блок_кода_занимает_строки_вместе_с_ограждением()
    {
        var code = ModelBuilder.Parse("```\nx\n```")[0];

        code.Source.StartLine.Should().Be(0);
        code.Source.EndLine.Should().Be(2);
    }

    [Fact]
    public void Отступ_знает_свою_пустую_строку()
    {
        var spacer = ModelBuilder.Parse("a\n\nb")[1];

        spacer.Should().BeOfType<BlankLineVm>();
        spacer.Source.StartLine.Should().Be(1);
        spacer.Source.EndLine.Should().Be(1);
    }

    [Theory]
    [InlineData("a **bold** c", "bold")]
    [InlineData("> quoted **bold**", "bold")]
    [InlineData("- item `code`", "`code`")]
    public void Кусок_текста_указывает_на_себя_в_исходнике(string text, string expected)
    {
        var run = Runs(ModelBuilder.Parse(text)).Single(run => run.Text == expected.Trim('`'));

        Slice(text, run.Source).Should().Be(expected);
    }

    [Fact]
    public void Пункты_списка_и_ячейки_таблицы_знают_свои_строки()
    {
        var list = (ListVm)ModelBuilder.Parse("- a\n- b")[0];
        list.Items.Select(item => item.Source.StartLine).Should().Equal(0, 1);

        var table = (TableVm)ModelBuilder.Parse("| a | b |\n|---|---|\n| 1 | 2 |")[0];
        table.Rows.Select(row => row.Source.StartLine).Should().Equal(0, 2);
        table.Rows[1].Cells.Select(cell => cell.Source.StartLine).Should().Equal(2, 2);
    }

    private static string Slice(string text, MdSourceSpan span) => text.Substring(span.Start, span.Length);

    private static IEnumerable<RunVm> Runs(IEnumerable<BlockVm> blocks)
    {
        return blocks.SelectMany(block => block switch
        {
            ParagraphVm paragraph => paragraph.Inlines.OfType<RunVm>(),
            QuoteVm quote => Runs(quote.Blocks),
            ListVm list => list.Items.SelectMany(item => Runs(item.Blocks)),
            _ => []
        });
    }
}
