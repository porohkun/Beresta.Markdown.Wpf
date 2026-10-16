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
    public void Блок_HTML_показывается_исходным_текстом()
    {
        ModelDump.Of("<section>html text</section>").Should().Be("P(<section>html text</section>)");
    }

    [Fact]
    public void Многострочный_блок_HTML_сохраняет_строки()
    {
        ModelDump.Of("<section>\ntext\n</section>").Should().Be("P(<section>\ntext\n</section>)");
    }
}