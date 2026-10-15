namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Controls;
using Identity;
using Markdig.Syntax;
using Presenters;
using Styling;
using ViewModels;

/// <summary>Новый текст: идентификаторы, вьюмодели, контейнеры и высоты неизменившихся блоков.</summary>
public sealed class IncrementalTests
{
    [Fact]
    public void Блоки_сохраняют_идентификаторы_после_вставки_выше()
    {
        var identity = new ModelIdentity();
        var before = ModelBuilder.Parse("One\n\nTwo\n\n- a\n- b", identity: identity);
        var after = ModelBuilder.Parse("Zero\n\nOne\n\nTwo\n\n- a\n- b", identity: identity);

        Paragraph(after, "One").NodeId.Should().Be(Paragraph(before, "One").NodeId);
        Paragraph(after, "Two").NodeId.Should().Be(Paragraph(before, "Two").NodeId);
        Items(after).Should().Equal(Items(before));
        Paragraph(after, "Zero").NodeId.Should().NotBe(0);
        before.Select(block => block.NodeId).Should().NotContain(Paragraph(after, "Zero").NodeId);
    }

    [Fact]
    public void Изменённый_на_месте_блок_сохраняет_идентификатор()
    {
        var identity = new ModelIdentity();
        var before = ModelBuilder.Parse("Intro\n\nHello", identity: identity);
        var after = ModelBuilder.Parse("Intro\n\nHello, world", identity: identity);

        Paragraph(after, "Hello, world").NodeId.Should().Be(Paragraph(before, "Hello").NodeId);
    }

    [Fact]
    public void Изменённый_пункт_сохраняет_идентификаторы_списка_и_пунктов()
    {
        var identity = new ModelIdentity();
        var before = ModelBuilder.Parse("- a\n- b\n- c", identity: identity);
        var after = ModelBuilder.Parse("- a\n- changed\n- c", identity: identity);

        Items(after).Should().Equal(Items(before));
        after[0].NodeId.Should().Be(before[0].NodeId);
    }

    [Fact]
    public void Одинаковые_блоки_сопоставляются_по_порядку()
    {
        var identity = new ModelIdentity();
        var before = ModelBuilder.Parse("A\n\nA\n\nA", identity: identity);
        var after = ModelBuilder.Parse("A\n\nB\n\nA\n\nA", identity: identity);

        var old = before.OfType<ParagraphVm>().Select(paragraph => paragraph.NodeId).ToList();
        after.OfType<ParagraphVm>().Where(paragraph => TextOf(paragraph) == "A").Select(paragraph => paragraph.NodeId).Should().Equal(old);
        old.Should().NotContain(Paragraph(after, "B").NodeId);
    }

    [Fact]
    public void Неизменный_блок_до_правки_остаётся_прежней_вьюмоделью()
    {
        var identity = new ModelIdentity();
        var before = ModelBuilder.Parse("One\n\nTwo", identity: identity);
        var after = ModelBuilder.Parse("One\n\nTwo, longer", identity: identity);

        Paragraph(after, "One").Should().BeSameAs(Paragraph(before, "One"));
        after[1].Should().BeOfType<BlankLineVm>().And.BeSameAs(before[1]);
    }

    [Fact]
    public void Сдвинутый_блок_получает_новую_позицию_при_прежнем_идентификаторе()
    {
        var identity = new ModelIdentity();
        var before = ModelBuilder.Parse("One\n\nTwo", identity: identity);
        var after = ModelBuilder.Parse("Zero\n\nOne\n\nTwo", identity: identity);

        var moved = Paragraph(after, "Two");
        var old = Paragraph(before, "Two");
        moved.Should().NotBeSameAs(old);
        moved.NodeId.Should().Be(old.NodeId);
        moved.Source.StartLine.Should().Be(old.Source.StartLine + 2);
    }

    [Fact]
    public void Новое_определение_ссылки_не_оставляет_прежнюю_вьюмодель()
    {
        var identity = new ModelIdentity();
        var before = ModelBuilder.Parse("[x][r]\n\n[r]: https://a.example", identity: identity);
        var after = ModelBuilder.Parse("[x][r]\n\n[r]: https://b.example", identity: identity);

        var paragraph = after.OfType<ParagraphVm>().Single();
        var old = before.OfType<ParagraphVm>().Single();
        paragraph.NodeId.Should().Be(old.NodeId);
        paragraph.Should().NotBeSameAs(old);
        paragraph.Inlines.OfType<RunVm>().Single().Href.Should().Be("https://b.example");
    }

    [Fact]
    public void Контейнер_и_вид_неизменного_блока_переживают_вставку_выше()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "One\n\nTwo\n\nThree" };
            var window = WpfHost.Show(viewer);
            var container = ContainerOf(viewer, "Two");
            var text = Visuals.Descendants<MdTextBlock>(container).Single();

            viewer.Markdown = "Zero\n\nOne\n\nTwo\n\nThree";
            WpfHost.Settle(window);

            ContainerOf(viewer, "Two").Should().BeSameAs(container);
            Visuals.Descendants<MdTextBlock>(container).Single().Should().BeSameAs(text);
            window.Close();
        });
    }

    [Fact]
    public void Дописанный_блок_остаётся_в_своём_контейнере()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "Intro\n\nHello" };
            var window = WpfHost.Show(viewer);
            var container = ContainerOf(viewer, "Hello");

            viewer.Markdown = "Intro\n\nHello, world";
            WpfHost.Settle(window);

            ContainerOf(viewer, "Hello, world").Should().BeSameAs(container);
            window.Close();
        });
    }

    [Fact]
    public void Виджет_с_состоянием_переживает_правки_вокруг()
    {
        WpfHost.Invoke(() =>
        {
            var template = TestWidgets.Template("<TextBox />");
            var input = new LambdaExtension
            {
                Block = (block, _) => block is FencedCodeBlock { Info: "input" } ? new InputVm() : null,
                Template = item => item is InputVm ? template : null
            };
            var viewer = new MarkdownViewer { Extensions = [input], Markdown = "Intro\n\n```input\n```\n\nOutro" };
            var window = WpfHost.Show(viewer);
            var box = Visuals.Descendants<TextBox>(viewer).Single();
            box.Text = "typed";

            viewer.Markdown = "Intro, longer\n\nNew paragraph\n\n```input\n```\n\nOutro, changed";
            WpfHost.Settle(window);

            Visuals.Descendants<TextBox>(viewer).Single().Should().BeSameAs(box);
            box.Text.Should().Be("typed");
            window.Close();
        });
    }

    [Fact]
    public void Видимый_блок_остаётся_на_месте_при_вставке_выше()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n\n", Enumerable.Range(0, 400).Select(i => $"Paragraph {i}"));
            var viewer = new MarkdownViewer { Markdown = text };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            viewer.ScrollToSourceLine(400).Should().BeTrue();
            WpfHost.Settle(window);
            var before = viewer.GetVisibleSourceLine()!.Value;

            viewer.Markdown = string.Join("\n\n", Enumerable.Range(0, 10).Select(i => $"Inserted {i}")) + "\n\n" + text;
            WpfHost.Settle(window);

            viewer.GetVisibleSourceLine().Should().BeApproximately(before + 20, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Смена_провайдера_стилей_перестраивает_блоки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "One\n\nTwo" };
            var window = WpfHost.Show(viewer);

            viewer.StyleProvider = new MdStyleProvider
            {
                BasedOn = viewer.StyleProvider,
                ParagraphTemplate = TestWidgets.Template("<Border Tag='custom' />")
            };
            WpfHost.Settle(window);

            Visuals.Descendants<Border>(viewer).Count(border => Equals(border.Tag, "custom")).Should().Be(2);
            window.Close();
        });
    }

    private static ParagraphVm Paragraph(IEnumerable<BlockVm> blocks, string text)
        => blocks.OfType<ParagraphVm>().Single(paragraph => TextOf(paragraph) == text);

    private static string TextOf(ParagraphVm paragraph)
        => string.Concat(paragraph.Inlines.OfType<RunVm>().Select(run => run.Text));

    private static IEnumerable<long> Items(IEnumerable<BlockVm> blocks)
        => blocks.OfType<ListVm>().Single().Items.Select(item => item.NodeId);

    private static FrameworkElement ContainerOf(MarkdownViewer viewer, string text)
        => (FrameworkElement)viewer.ItemContainerGenerator.ContainerFromItem(Paragraph(viewer.Items.OfType<BlockVm>(), text));

    private sealed record InputVm : BlockVm;
}
