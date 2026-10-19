namespace Beresta.Markdown.Wpf.Tests;

using ViewModels;

/// <summary>Копия записи модели через <c>with</c> согласована сама с собой.</summary>
public sealed class ModelCopyTests
{
    [Fact]
    public void Новый_код_получает_новый_кусок_текста()
    {
        var code = new CodeBlockVm(null, null, "old code");

        var changed = code with { Code = "new code" };

        code.Runs.Should().ContainSingle().Which.As<RunVm>().Text.Should().Be("old code");
        changed.Runs.Should().ContainSingle().Which.As<RunVm>().Text.Should().Be("new code");
    }

    [Fact]
    public void Пустой_список_инлайнов_у_каждой_ячейки_свой()
    {
        var cell = new TableCellVm([new RunVm("x")]);

        var first = cell with { Inlines = [] };
        var second = cell with { Inlines = [] };

        first.Inlines.Should().BeEmpty().And.NotBeSameAs(second.Inlines);
    }
}
