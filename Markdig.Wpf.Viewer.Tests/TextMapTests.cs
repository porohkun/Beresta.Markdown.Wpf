namespace MarkdigWpfViewer.Tests;

using Selection;
using ViewModels;

/// <summary>Текстовая карта: сквозной текст документа и куски его частей.</summary>
public sealed class TextMapTests
{
    [Theory]
    [InlineData("a\n\nb", "a\n\nb")]
    [InlineData("# H\ntext", "H\ntext")]
    [InlineData("- a\n- b", "a\nb")]
    [InlineData("| a | b |\n|---|---|\n| 1 | 2 |", "a\tb\n1\t2")]
    [InlineData("```\nx\ny\n```", "x\ny")]
    [InlineData("> a\n>\n> b", "a\n\nb")]
    [InlineData("use `foo()` now", "use foo() now")]
    [InlineData("a  \nb", "a\nb")]
    public void Текст_карты_читается_как_документ(string markdown, string expected)
    {
        var map = MdTextMap.Build(ModelBuilder.Parse(markdown));

        map.GetText(new(0, map.Length)).Should().Be(expected);
    }

    [Fact]
    public void Неделимый_блок_занимает_один_знак_и_не_копируется()
    {
        var blocks = ModelBuilder.Parse("a\n\n---\n\nb");
        var map = MdTextMap.Build(blocks);

        map.TryGetRange(blocks.OfType<ThematicBreakVm>().Single(), out var range).Should().BeTrue();
        range.Length.Should().Be(1);
        map.Text[range.Start].Should().Be(MdTextMap.ObjectMark);
        map.GetText(new(0, map.Length)).Should().NotContain(MdTextMap.ObjectMark.ToString());
    }

    [Fact]
    public void Кусок_списка_инлайнов_указывает_на_его_текст()
    {
        var blocks = ModelBuilder.Parse("first\n\nsecond **bold**");
        var map = MdTextMap.Build(blocks);
        var paragraph = (ParagraphVm)blocks[2];

        map.TryGetRange(paragraph.Inlines, out var range).Should().BeTrue();
        map.GetText(range).Should().Be("second bold");
    }

    [Fact]
    public void Виджет_с_текстом_отдаёт_свой_текст_и_вложенные_блоки()
    {
        var note = new TextNoteVm("Title", ModelBuilder.Parse("inner **text**"));
        var map = MdTextMap.Build([note]);

        map.Text.Should().Be("Title\ninner text");
        map.TryGetRange(note, out _).Should().BeFalse("виджет с текстом не неделимый");
    }

    [Fact]
    public void Виджет_без_текста_неделимый()
    {
        var widget = new TestWidgets.DiagramVm("A->B");
        var map = MdTextMap.Build([widget]);

        map.TryGetRange(widget, out var range).Should().BeTrue();
        range.Should().Be(new MdTextRange(0, 1));
    }

    [Fact]
    public void Строчный_виджет_без_текста_занимает_один_знак()
    {
        var inlines = new InlineVm[] { new RunVm("a "), new TestWidgets.MentionVm("x"), new RunVm(" b") };
        var map = MdTextMap.Build([new ParagraphVm(inlines)]);

        map.Text.Should().Be($"a {MdTextMap.ObjectMark} b");
    }

    [Fact]
    public void Неделимый_виджет_копируется_своим_текстом()
    {
        var mention = new TestWidgets.MentionVm("ivan");
        var map = MdTextMap.Build([new ParagraphVm([new RunVm("a "), mention, new RunVm(" b")]), new CopyDiagramVm()]);

        map.Text.Should().Be($"a {MdTextMap.ObjectMark} b\n{MdTextMap.ObjectMark}");
        map.GetText(new(0, map.Length)).Should().Be("a @ivan b\n[diagram]");
        map.GetText(new(2, 1)).Should().Be("@ivan");
    }

    [Fact]
    public void Блоки_карты_идут_в_порядке_чтения_вместе_с_вложенными()
    {
        var blocks = ModelBuilder.Parse("> a\n\nb");
        var map = MdTextMap.Build(blocks);

        map.Blocks.Select(entry => entry.Block.GetType().Name)
            .Should().Equal(nameof(QuoteVm), nameof(ParagraphVm), nameof(BlankLineVm), nameof(ParagraphVm));
    }

    private sealed record CopyDiagramVm : BlockVm, IMdCopyText
    {
        public string CopyText => "[diagram]";
    }

    private sealed record TextNoteVm(string Title, IReadOnlyList<BlockVm> Blocks) : BlockVm, IMdTextSource
    {
        public void WriteText(MdTextWriter writer)
        {
            writer.Text(Title);
            writer.Text("\n");
            writer.Blocks(Blocks);
        }
    }
}