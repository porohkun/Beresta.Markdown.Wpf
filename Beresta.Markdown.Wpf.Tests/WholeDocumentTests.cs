namespace Beresta.Markdown.Wpf.Tests;

using ViewModels;

/// <summary>Конструкции, которым нужен контекст через пустую строку, разбираются целиком.</summary>
public sealed class WholeDocumentTests
{
    [Fact]
    public void Ссылка_по_метке_находит_адрес_через_пустую_строку()
    {
        var blocks = ModelBuilder.Parse("see [docs][d]\n\n[d]: https://example.com");

        ModelDump.Of(blocks).Should().Be("P(see docs)");
        var link = ((ParagraphVm)blocks[0]).Inlines.OfType<RunVm>().Single(inline => inline.Text == "docs");
        link.Href.Should().Be("https://example.com");
    }

    [Fact]
    public void Второй_абзац_пункта_остаётся_в_пункте()
    {
        ModelDump.Of("- first\n\n  second").Should().Be("L[• P(first) _ P(second)]");
    }

    [Fact]
    public void Код_в_пункте_остаётся_в_пункте()
    {
        ModelDump.Of("- item\n\n      code").Should().Be("L[• P(item) _ C(code)]");
    }

    [Fact]
    public void Ограждение_тильдами_не_закрывается_обратными_кавычками()
    {
        ModelDump.Of("~~~\n```\n\nstill code\n~~~").Should().Be("C(```\n\nstill code)");
    }

    [Fact]
    public void Текст_сноски_не_теряется()
    {
        ModelDump.Of("text[^1]\n\n[^1]: note").Should().Contain("P(note");
    }
}