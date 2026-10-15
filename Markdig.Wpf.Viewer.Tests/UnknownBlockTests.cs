namespace MarkdigWpfViewer.Tests;

/// <summary>Блоки без своего вида не теряются: рисуется содержимое либо исходный текст.</summary>
public sealed class UnknownBlockTests
{
    [Fact]
    public void Содержимое_контейнера_не_теряется()
    {
        ModelDump.Of("::: note\nважный текст\n:::").Should().Be("P(важный текст)");
    }

    [Fact]
    public void Список_определений_не_теряется()
    {
        ModelDump.Of("Term\n:   Definition").Should().Be("P(Term) P(Definition)");
    }

    [Fact]
    public void Блок_HTML_показывается_исходным_текстом()
    {
        ModelDump.Of("<div>html text</div>").Should().Be("P(<div>html text</div>)");
    }

    [Fact]
    public void Многострочный_блок_HTML_сохраняет_строки()
    {
        ModelDump.Of("<div>\ntext\n</div>").Should().Be("P(<div>\ntext\n</div>)");
    }
}