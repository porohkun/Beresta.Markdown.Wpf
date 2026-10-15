namespace MarkdigWpfViewer.Tests;

using ViewModels;

/// <summary>Инлайны: начертания и то, для чего своего вида нет.</summary>
public sealed class InlineTests
{
    [Fact]
    public void Двойная_тильда_зачёркивает_а_не_выделяет_жирным()
    {
        var paragraph = (ParagraphVm)ModelBuilder.Parse("~~s~~")[0];

        paragraph.Inlines.Should().ContainSingle()
            .Which.Should().Match<InlineVm>(inline => inline.Strike && !inline.Bold && !inline.Italic);
    }

    [Fact]
    public void Спецсимвол_показывается_расшифрованным()
    {
        ModelDump.Of("Tom &amp; Jerry &copy;").Should().Be("P(Tom & Jerry ©)");
    }

    [Fact]
    public void Тег_в_строке_показывается_как_есть()
    {
        ModelDump.Of("a <b>bold</b> c").Should().Be("P(a <b>bold</b> c)");
    }

    [Fact]
    public void Формула_в_строке_показывается_исходным_текстом()
    {
        ModelDump.Of("x $a+b$ y").Should().Be("P(x $a+b$ y)");
    }

    [Fact]
    public void Непарная_скобка_не_теряется()
    {
        ModelDump.Of("a [b c").Should().Be("P(a [b c)");
    }

    [Fact]
    public void Ссылка_по_метке_без_адреса_остаётся_текстом()
    {
        ModelDump.Of("a [b] c").Should().Be("P(a [b] c)");
    }
}
