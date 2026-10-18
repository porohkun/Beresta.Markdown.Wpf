namespace Beresta.Markdown.Wpf.Tests;

using Markdig.Syntax;
using ViewModels;
using static TestWidgets;

/// <summary>Расширения превращают свой синтаксис в виджеты; остальное разбирается как обычно.</summary>
public sealed class ExtensionTests
{
    [Fact]
    public void Свой_контейнер_становится_виджетом_с_разобранным_содержимым()
    {
        var note = Note();
        var blocks = ModelBuilder.Parse(":::note Внимание\nтекст **жирный**\n\n- пункт\n:::", ModelDump.Pipeline(note), [note]);

        var widget = blocks.Should().ContainSingle().Which.Should().BeOfType<NoteVm>().Subject;
        widget.Title.Should().Be("Внимание");
        ModelDump.Of(widget.Blocks).Should().Be("P(текст жирный) _ L[• P(пункт)]");
        widget.Source.StartLine.Should().Be(0);
    }

    [Fact]
    public void Расширение_перекрывает_встроенный_блок()
    {
        const string text = "```diagram\nA->B\n```\n\n```\ncode\n```";
        var blocks = ModelBuilder.Parse(text, ModelDump.Pipeline(), [Diagram()]);

        ModelDump.Of(blocks).Should().Be("{DiagramVm} _ C(code)");
        ((DiagramVm)blocks[0]).Code.Should().Be("```diagram\nA->B\n```");
    }

    [Fact]
    public void Строчный_виджет_из_узла_который_знает_Markdig()
    {
        ModelDump.Of("- [x] done", Check()).Should().Be("L[• P({CheckVm} done)]");
    }

    [Fact]
    public void Свой_синтаксис_приходит_через_настройку_разбора()
    {
        const string text = "привет @ivan!";
        var paragraph = (ParagraphVm)ModelBuilder.Parse(text, ModelDump.Pipeline(Mention()), [Mention()])[0];

        ModelDump.Of([paragraph]).Should().Be("P(привет {MentionVm}!)");
        var mention = paragraph.Inlines.OfType<MentionVm>().Single();
        mention.Name.Should().Be("ivan");
        text.Substring(mention.Source.Start, mention.Source.Length).Should().Be("@ivan");
    }

    [Fact]
    public void Первое_расширение_по_порядку_выигрывает()
    {
        var first = new LambdaExtension { Block = (block, _) => block is ParagraphBlock ? new DiagramVm("first") : null };
        var second = new LambdaExtension { Block = (block, _) => block is ParagraphBlock ? new DiagramVm("second") : null };

        var blocks = ModelBuilder.Parse("a", ModelDump.Pipeline(), [first, second]);

        blocks.Should().ContainSingle().Which.Should().BeOfType<DiagramVm>().Which.Code.Should().Be("first");
    }

    [Fact]
    public void Чужие_узлы_разбираются_как_обычно()
    {
        ModelDump.Of("# H\n\ntext", Note(), Check()).Should().Be("H1(H) _ P(text)");
    }
}