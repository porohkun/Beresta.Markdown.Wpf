namespace Beresta.Markdown.Wpf.Tests;

using ViewModels;

/// <summary>Глубокая вложенность не роняет вьюер: глубже предела она показывается исходным текстом.</summary>
public sealed class NestingTests
{
    private const string Div = "<div align=\"center\">";

    [Fact]
    public void Сто_двадцать_цитат_показываются()
    {
        Shows(new string('>', 120) + " deep", "deep");
    }

    [Fact]
    public void Двести_выровненных_блоков_HTML_показываются()
    {
        Shows(Repeat(Div + "\n", 200) + "deep\n" + Repeat("</div>\n", 200), "deep");
    }

    [Fact]
    public void Пять_тысяч_звёздочек_вокруг_текста_показываются()
    {
        Shows(new string('*', 5000) + "deep" + new string('*', 5000), "deep");
    }

    [Fact]
    public void Сто_тысяч_звёздочек_вокруг_текста_показываются()
    {
        Shows(new string('*', 100_000) + "deep" + new string('*', 100_000), "deep");
    }

    [Fact]
    public void Сорок_уровней_списка_показываются()
    {
        Shows(string.Join("\n", Enumerable.Range(1, 40).Select(level => new string(' ', 2 * (level - 1)) + "- item" + level)), "item40");
    }

    [Fact]
    public void Цитаты_до_предела_разворачиваются()
    {
        ModelDump.Of(new string('>', ModelBuilder.MaxDepth) + " **deep**")
            .Should().Be(Repeat("Q[", ModelBuilder.MaxDepth) + "P(deep)" + new string(']', ModelBuilder.MaxDepth));
    }

    [Fact]
    public void Цитаты_глубже_предела_показываются_исходником()
    {
        ModelDump.Of(new string('>', 40) + " **deep**")
            .Should().Be(Repeat("Q[", ModelBuilder.MaxDepth) + "P(>>>>>>>> **deep**)" + new string(']', ModelBuilder.MaxDepth));
    }

    [Fact]
    public void Строки_контейнера_глубже_предела_теряют_префикс_внешних()
    {
        var quotes = new string('>', 40);

        ModelDump.Of($"{quotes} first\n{quotes} second\nlazy")
            .Should().Be(Repeat("Q[", ModelBuilder.MaxDepth) + "P(>>>>>>>> first\n>>>>>>>> second\nlazy)" + new string(']', ModelBuilder.MaxDepth));
    }

    [Fact]
    public void Пункты_списка_глубже_предела_сохраняют_свои_отступы()
    {
        var markdown = string.Join("\n", Enumerable.Range(1, 35).Select(level => new string(' ', 2 * (level - 1)) + "- item" + level));
        var item = ((ListVm)ModelBuilder.Parse(markdown)[0]).Items[0];
        for (var level = 2; level <= ModelBuilder.MaxDepth; level++)
        {
            item = ((ListVm)item.Blocks[1]).Items[0];
        }

        ModelDump.Of(item.Blocks.Skip(1)).Should().Be("P(- item33\n  - item34\n    - item35)");
    }

    [Fact]
    public void Блоки_HTML_глубже_предела_показываются_исходником()
    {
        var inner = string.Join("\n", Enumerable.Repeat(Div, 8).Append("deep").Concat(Enumerable.Repeat("</div>", 8)));

        ModelDump.Of(Repeat(Div + "\n", 40) + "deep\n" + Repeat("</div>\n", 40))
            .Should().Be(Repeat("Center[", ModelBuilder.MaxDepth) + $"P({inner})" + new string(']', ModelBuilder.MaxDepth));
    }

    [Fact]
    public void Выделение_до_предела_разворачивается()
    {
        var stars = new string('*', 2 * ModelBuilder.MaxInlineDepth);

        var run = ((ParagraphVm)ModelBuilder.Parse(stars + "deep" + stars)[0]).Inlines.Should().ContainSingle().Which.As<RunVm>();

        run.Text.Should().Be("deep");
        run.Bold.Should().BeTrue();
    }

    [Fact]
    public void Выделение_глубже_предела_показывается_исходником()
    {
        var rest = new string('*', 400 - 2 * ModelBuilder.MaxInlineDepth);

        var run = ((ParagraphVm)ModelBuilder.Parse(new string('*', 400) + "deep" + new string('*', 400))[0]).Inlines.Should().ContainSingle().Which.As<RunVm>();

        run.Text.Should().Be(rest + "deep" + rest);
        run.Bold.Should().BeTrue();
    }

    [Fact]
    public void Подпись_картинки_с_глубоким_выделением_собирается()
    {
        var stars = new string('*', 100_000);

        var image = ((ParagraphVm)ModelBuilder.Parse($"![{stars}deep{stars}](picture.png)")[0]).Inlines.OfType<ImageVm>().Single();

        image.Alt.Should().Be("deep");
    }

    [Fact]
    public void Глубокий_документ_разбирается_повторно_с_прежними_идентификаторами()
    {
        var markdown = new string('>', 40) + " deep\n\n" + new string('*', 100_000) + "deep" + new string('*', 100_000);
        var identity = new Identity.ModelIdentity();

        var first = ModelBuilder.Parse(markdown, identity: identity);
        var second = ModelBuilder.Parse(markdown, identity: identity);

        second.Should().Equal(first, (left, right) => ReferenceEquals(left, right));
    }

    private static string Repeat(string text, int count) => string.Concat(Enumerable.Repeat(text, count));

    private static void Shows(string markdown, string text)
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = markdown };
            var window = WpfHost.Show(viewer);

            viewer.TextMap.Text.Should().Contain(text);
            window.Close();
        });
    }
}
