namespace Beresta.Markdown.Wpf.Tests;

using Selection;
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
    public void Код_с_отступом_занимает_только_свои_строки()
    {
        const string text = "para\n\n    code1\n    code2\n\n\n\npara2";
        var blocks = ModelBuilder.Parse(text);
        var code = blocks.OfType<CodeBlockVm>().Single();
        var map = MdTextMap.Build(blocks, text);

        code.IsFenced.Should().BeFalse();
        code.Source.StartLine.Should().Be(2);
        code.Source.EndLine.Should().Be(3);
        ModelDump.Of(blocks).Should().Be("P(para) _ C(code1\ncode2) _ _ _ P(para2)");
        map.SourceLineOf(map.Text.IndexOf("code1", StringComparison.Ordinal)).Should().Be(2);
    }

    [Fact]
    public void Пустые_строки_перед_оградой_остаются_в_коде()
    {
        const string text = "```\ncode\n\n\n```";
        var blocks = ModelBuilder.Parse(text);
        var code = (CodeBlockVm)blocks[0];
        var map = MdTextMap.Build(blocks, text);

        code.Code.Should().Be("code\n\n");
        code.IsFenced.Should().BeTrue();
        code.Source.EndLine.Should().Be(4);
        map.SourceLineOf(map.Text.Length).Should().Be(3);
    }

    [Fact]
    public void Незакрытая_ограда_кончается_последней_строкой_кода()
    {
        var code = ModelBuilder.Parse("text\n\n```\ncode\n\n\n").OfType<CodeBlockVm>().Single();

        code.Code.Should().Be("code");
        code.IsFenced.Should().BeTrue();
        code.Source.StartLine.Should().Be(2);
        code.Source.EndLine.Should().Be(3);
    }

    [Theory]
    [InlineData("> [!WARNING]\n> First line\n> Second line", "First line\n> Second line")]
    [InlineData("> [!NOTE]\nLazy first line", "Lazy first line")]
    public void Абзац_оповещения_начинается_со_строки_своего_текста(string text, string expected)
    {
        var paragraph = ((CalloutVm)ModelBuilder.Parse(text)[0]).Blocks[0];

        paragraph.Source.StartLine.Should().Be(1);
        Slice(text, paragraph.Source).Should().Be(expected);
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