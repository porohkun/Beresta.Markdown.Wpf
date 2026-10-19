namespace Beresta.Markdown.Wpf.Tests;

using System.Windows.Controls;
using System.Windows.Documents;
using Presenters;
using ViewModels;

/// <summary>Инлайны: начертания и то, для чего своего вида нет.</summary>
public sealed class InlineTests
{
    [Fact]
    public void Двойная_тильда_зачёркивает_а_не_выделяет_жирным()
    {
        var paragraph = (ParagraphVm)ModelBuilder.Parse("~~s~~")[0];

        paragraph.Inlines.Should().ContainSingle()
            .Which.Should().Match<RunVm>(inline => inline.Strike && !inline.Bold && !inline.Italic);
    }

    [Fact]
    public void Автоссылка_в_угловых_скобках_становится_ссылкой()
    {
        var paragraph = (ParagraphVm)ModelBuilder.Parse("see <https://example.com>")[0];

        paragraph.Inlines.OfType<RunVm>().Should().ContainSingle(run => run.Href == "https://example.com")
            .Which.Text.Should().Be("https://example.com");
    }

    [Fact]
    public void Почтовая_автоссылка_ведёт_на_mailto()
    {
        var paragraph = (ParagraphVm)ModelBuilder.Parse("write <user@example.com>")[0];

        paragraph.Inlines.OfType<RunVm>().Should().ContainSingle(run => run.Href == "mailto:user@example.com")
            .Which.Text.Should().Be("user@example.com");
    }

    [Fact]
    public void Ссылка_на_заголовок_по_его_тексту_ведёт_к_якорю()
    {
        var paragraph = ModelBuilder.Parse("# My Heading\n\nSee [My Heading].").OfType<ParagraphVm>().Single();

        paragraph.Inlines.OfType<RunVm>().Should().ContainSingle(run => run.Href == "#my-heading")
            .Which.Text.Should().Be("My Heading");
    }

    [Fact]
    public void Каждая_строка_исходника_начинается_с_новой_строки()
    {
        ModelDump.Of("first\nsecond *third\nfourth*").Should().Be("P(first\nsecond third\nfourth)");
    }

    [Fact]
    public void Спецсимвол_показывается_расшифрованным()
    {
        ModelDump.Of("Tom &amp; Jerry &copy;").Should().Be("P(Tom & Jerry ©)");
    }

    [Fact]
    public void Незнакомый_тег_в_строке_показывается_как_есть()
    {
        ModelDump.Of("a <span>x</span> c").Should().Be("P(a <span>x</span> c)");
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

    [Fact]
    public void В_режиме_склейки_мягкий_перенос_становится_пробелом_а_жёсткий_остаётся()
    {
        ModelDump.Of(ModelBuilder.Parse("a\nb", null, null, null, out _, joinSoftLineBreaks: true)).Should().Be("P(a b)");
        ModelDump.Of(ModelBuilder.Parse("a  \nb", null, null, null, out _, joinSoftLineBreaks: true)).Should().Be("P(a\nb)");
    }

    [Fact]
    public void Вьюер_в_режиме_склейки_рисует_абзац_одной_строкой()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "a\nb" };
            var host = new Border { Child = viewer };
            MarkdownViewer.SetJoinSoftLineBreaks(host, true);
            var window = WpfHost.Show(host);
            var text = viewer.TextMap.Text;

            text.Should().Be("a b");
            Visuals.Descendants<MdTextBlock>(viewer).Single().Inlines.OfType<LineBreak>().Should().BeEmpty();
            viewer.TextMap.SourceLineOf(text.IndexOf('b')).Should().Be(1);
            viewer.SelectAll();
            viewer.SelectedMarkdown.Should().Be("a b");

            MarkdownViewer.SetJoinSoftLineBreaks(host, false);
            WpfHost.Settle(window);
            viewer.TextMap.Text.Should().Be("a\nb");
            window.Close();
        });
    }
}