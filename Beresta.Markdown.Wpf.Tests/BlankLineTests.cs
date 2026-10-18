namespace Beresta.Markdown.Wpf.Tests;

/// <summary>Пустые строки исходника между блоками становятся отступами.</summary>
public sealed class BlankLineTests
{
    [Fact]
    public void Пустая_строка_между_абзацами_становится_отступом()
    {
        ModelDump.Of("a\n\nb").Should().Be("P(a) _ P(b)");
    }

    [Fact]
    public void Каждая_пустая_строка_даёт_свой_отступ()
    {
        ModelDump.Of("a\n\n\nb").Should().Be("P(a) _ _ P(b)");
    }

    [Fact]
    public void Блоки_без_пустой_строки_идут_вплотную()
    {
        ModelDump.Of("# H\ntext").Should().Be("H1(H) P(text)");
    }

    [Theory]
    [InlineData("```\nx\n```\n\nb", "C(x) _ P(b)")]
    [InlineData("Title\n===\n\nb", "H1(Title) _ P(b)")]
    [InlineData("| a |\n|---|\n| 1 |\n\nb", "T(2x1) _ P(b)")]
    [InlineData("- a\n  more\n\nb", "L[• P(a\nmore)] _ P(b)")]
    [InlineData("> a\n> b\n\nc", "Q[P(a\nb)] _ P(c)")]
    public void Отступ_после_многострочного_блока_считается_от_его_конца(string markdown, string expected)
    {
        ModelDump.Of(markdown).Should().Be(expected);
    }

    [Theory]
    [InlineData("a\n\n[d]: https://example.com\n\nb", "P(a) _ _ P(b)")]
    [InlineData("a[^1]\n\n[^1]: one\n\nb", "P(a^1) _ F1[P(one↩)] _ P(b)")]
    [InlineData("::: note\ntext\n:::\n\nb", "P(text) _ P(b)")]
    public void Невидимые_строки_исходника_отступами_не_становятся(string markdown, string expected)
    {
        ModelDump.Of(markdown).Should().Be(expected);
    }

    [Fact]
    public void Пустые_строки_по_краям_документа_отступов_не_дают()
    {
        ModelDump.Of("\n\na\n\n").Should().Be("P(a)");
    }

    [Fact]
    public void Переводы_строк_Windows_считаются_так_же()
    {
        ModelDump.Of("a\r\n\r\nb\r\n").Should().Be("P(a) _ P(b)");
    }

    [Fact]
    public void Пустая_строка_в_цитате_становится_отступом_внутри_неё()
    {
        ModelDump.Of("> a\n>\n> b").Should().Be("Q[P(a) _ P(b)]");
    }

    [Fact]
    public void Пустые_строки_внутри_кода_остаются_кодом()
    {
        ModelDump.Of("```\na\n\nb\n```").Should().Be("C(a\n\nb)");
    }
}