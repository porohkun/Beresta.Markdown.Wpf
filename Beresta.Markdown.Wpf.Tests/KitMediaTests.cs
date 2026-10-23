namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Extensions;
using Kit;
using Presenters;
using Selection;
using ViewModels;

/// <summary>Готовые расширения медиа и данных: карточки видео, эмодзи, шорткоды, таблицы из CSV, карточка свойств.</summary>
public sealed class KitMediaTests
{
    [Theory]
    [InlineData("[![Talk](cover.png)](https://www.youtube.com/watch?v=abc123xyz)", "https://www.youtube.com/watch?v=abc123xyz", "Talk", "cover.png")]
    [InlineData("![Talk](https://youtu.be/abc123xyz)", "https://youtu.be/abc123xyz", "Talk", "https://img.youtube.com/vi/abc123xyz/hqdefault.jpg")]
    [InlineData("![Short](https://www.youtube.com/shorts/abc123xyz)", "https://www.youtube.com/shorts/abc123xyz", "Short", "https://img.youtube.com/vi/abc123xyz/hqdefault.jpg")]
    [InlineData("![Clip](https://vimeo.com/123456)", "https://vimeo.com/123456", "Clip", null)]
    [InlineData("[![Clip](thumb.jpg)](https://rutube.ru/video/0a1b2c/)", "https://rutube.ru/video/0a1b2c/", "Clip", "thumb.jpg")]
    public void Картинка_со_ссылкой_на_видео_становится_карточкой(string markdown, string href, string title, string? preview)
    {
        var card = Inlines(markdown, new VideoCards()).OfType<VideoCardVm>().Single();

        (card.Href, card.Title, card.Preview?.Url).Should().Be((href, title, preview));
    }

    [Theory]
    [InlineData("[![x](a.png)](https://example.com/watch?v=1)")]
    [InlineData("![x](https://example.com/clip.png)")]
    [InlineData("[text](https://www.youtube.com/watch?v=abc123xyz)")]
    public void Не_видео_остаётся_как_было(string markdown)
    {
        Inlines(markdown, new VideoCards()).OfType<VideoCardVm>().Should().BeEmpty();
    }

    [Fact]
    public void Без_загрузки_из_сети_у_карточки_нет_превью_из_сети()
    {
        var cards = Inlines(
                "![A](https://youtu.be/abc123xyz) [![B](local.png)](https://youtu.be/abc123xyz)",
                new VideoCards { LoadThumbnails = false })
            .OfType<VideoCardVm>()
            .ToList();

        cards.Select(card => card.Preview?.Url).Should().Equal(null, "local.png");
    }

    [Fact]
    public void Карточка_видео_пишется_в_разметку_как_была()
    {
        const string markdown = "Watch [![Talk](cover.png)](https://youtu.be/abc123xyz) now";

        Copy(markdown, new VideoCards()).Should().Be(markdown);
    }

    [Fact]
    public void Эмодзи_шорткод_становится_знаком_а_смайлик_остаётся_текстом()
    {
        ModelDump.Of("Hi :smile: and :) and :unknown:", new EmojiShortcodes()).Should().Be("P(Hi 😄 and :) and :unknown:)");
    }

    [Theory]
    [InlineData("{{< youtube abc123xyz >}}")]
    [InlineData("{% youtube abc123xyz %}")]
    [InlineData("{{% youtube id=\"abc123xyz\" %}}")]
    [InlineData("{%- youtube abc123xyz -%}")]
    public void Шорткод_видео_становится_карточкой(string markdown)
    {
        var card = Inlines(markdown, new Shortcodes()).OfType<VideoCardVm>().Single();

        card.Href.Should().Be("https://www.youtube.com/watch?v=abc123xyz");
        card.Preview!.Url.Should().Be("https://img.youtube.com/vi/abc123xyz/hqdefault.jpg");
    }

    [Theory]
    [InlineData("{{< vimeo 123456 >}}", "https://vimeo.com/123456", "Vimeo")]
    [InlineData("{{< gist octo 0123abc >}}", "https://gist.github.com/octo/0123abc", "Gist octo/0123abc")]
    [InlineData("{% gist octo/0123abc %}", "https://gist.github.com/octo/0123abc", "Gist octo/0123abc")]
    [InlineData("{{< pdf \"docs/guide.pdf\" >}}", "docs/guide.pdf", "guide.pdf")]
    public void Встроенные_шорткоды_становятся_карточками(string markdown, string href, string title)
    {
        var card = Inlines(markdown, new Shortcodes()).Single() switch
        {
            VideoCardVm video => (video.Href, video.Title),
            LinkCardVm link => (link.Href, link.Title),
            var other => (other.GetType().Name, string.Empty)
        };

        card.Should().Be((href, title));
    }

    [Fact]
    public void Шорткод_оглавления_становится_оглавлением_на_месте()
    {
        Parse("# A\n\n{{< toc >}}\n\n## B", new Shortcodes()).OfType<TocVm>().Should().ContainSingle();
    }

    [Fact]
    public void Шорткод_среди_текста_становится_виджетом_в_строке()
    {
        ModelDump.Text(Inlines("see {{< youtube abc123xyz >}} here", new Shortcodes())).Should().Be("see {VideoCardVm} here");
    }

    [Theory]
    [InlineData("{{< unknown x >}}")]
    [InlineData("{{< toc >}} inside text")]
    [InlineData("{% youtube %}")]
    [InlineData("{% unclosed")]
    public void Шорткод_без_обработчика_остаётся_текстом(string markdown)
    {
        ModelDump.Of(markdown, new Shortcodes()).Should().Be($"P({markdown})");
    }

    [Fact]
    public void Шорткод_в_коде_остаётся_кодом()
    {
        Inlines("`{{< youtube abc123xyz >}}`", new Shortcodes()).OfType<VideoCardVm>().Should().BeEmpty();
    }

    [Fact]
    public void Приложение_регистрирует_свои_шорткоды()
    {
        var shortcodes = new Shortcodes();
        shortcodes.Handlers["diagram"] = shortcode => new TestWidgets.DiagramVm(shortcode.Argument("src", 0) ?? string.Empty);
        shortcodes.Handlers["badge"] = shortcode => new RunVm($"{shortcode.Argument("text", 0)}/{shortcode.Argument("color", 1)}");

        var blocks = Parse("{{< diagram src=\"flow.mmd\" >}}\n\nA {{< badge text='new one' color=red >}} b", shortcodes);

        blocks.OfType<TestWidgets.DiagramVm>().Single().Code.Should().Be("flow.mmd");
        ModelDump.Text(blocks.OfType<ParagraphVm>().Single().Inlines).Should().Be("A new one/red b");
    }

    [Fact]
    public void Шорткод_пишется_в_разметку_как_был()
    {
        const string markdown = "see {{< youtube abc123xyz >}} and {% pdf a.pdf %}";

        Copy(markdown, new Shortcodes()).Should().Be(markdown);
    }

    [Fact]
    public void Блок_csv_становится_таблицей_с_заголовком()
    {
        const string markdown = "```csv\nname, age\n\"Smith, J\", 42\n\"say \"\"hi\"\"\",7\n```";

        var table = Parse(markdown, new CsvTables()).OfType<TableVm>().Single();

        table.ColumnCount.Should().Be(2);
        table.Rows.Select(row => row.IsHeader).Should().Equal(true, false, false);
        table.Rows.Select(row => string.Join("|", row.Cells.Select(cell => ModelDump.Text(cell.Inlines))))
            .Should().Equal("name|age", "Smith, J|42", "say \"hi\"|7");
    }

    [Fact]
    public void Значение_csv_в_кавычках_переносится_на_новую_строку()
    {
        const string markdown = "Text\n\n```CSV\na,b\n\"line1\nline2\",x\nlast,y\n```";

        var table = Parse(markdown, new CsvTables()).OfType<TableVm>().Single();

        ModelDump.Text(table.Rows[1].Cells[0].Inlines).Should().Be("line1\nline2");
        table.Rows.Select(row => (row.Source.StartLine, row.Source.EndLine)).Should().Equal((3, 3), (4, 5), (6, 6));
    }

    [Theory]
    [InlineData("```tsv\na\tb\n1\t2\n```", 2, 2)]
    [InlineData("```csv\na,b,c\n1\n```", 2, 3)]
    [InlineData("```csv\n\na,b\n\n1,2\n```", 2, 2)]
    public void Таблица_из_csv_дополняется_до_ширины(string markdown, int rows, int columns)
    {
        var table = Parse(markdown, new CsvTables()).OfType<TableVm>().Single();

        (table.Rows.Count, table.ColumnCount).Should().Be((rows, columns));
        table.Rows.Should().OnlyContain(row => row.Cells.Count == columns);
    }

    [Theory]
    [InlineData("```json\na,b\n```")]
    [InlineData("```csv\n```")]
    [InlineData("    a,b")]
    public void Другой_код_остаётся_кодом(string markdown)
    {
        Parse(markdown, new CsvTables()).Should().ContainSingle().Which.Should().BeOfType<CodeBlockVm>();
    }

    [Fact]
    public void Таблица_из_csv_пишется_в_разметку_таблицей()
    {
        Copy("```csv\nname,age\nSmith,42\n```", new CsvTables()).Should().Be("| name | age |\n| --- | --- |\n| Smith | 42 |");
    }

    [Fact]
    public void Front_matter_становится_карточкой_свойств()
    {
        const string markdown = """
            ---
            title: "Hello: world" # comment
            tags: [a, 'b c', "d"]
            author:
              name: Ivan
              url: https://example.com
            list:
              - one
              - 'two'
            plain:
            - x
            - y
            note: 'It''s'
            description: |
              Line 1
              Line 2
            folded: >
              one
              two
            empty:
            ---

            Text
            """;

        var blocks = Parse(markdown.ReplaceLineEndings("\n"), new PropertiesCard());

        var card = blocks.OfType<PropertiesVm>().Single();
        card.Items.Select(item => (ModelDump.Text(item.Key), ModelDump.Text(item.Value))).Should().Equal(
            ("title", "Hello: world"),
            ("tags", "a, b c, d"),
            ("author", "name: Ivan\nurl: https://example.com"),
            ("list", "one, two"),
            ("plain", "x, y"),
            ("note", "It's"),
            ("description", "Line 1\nLine 2"),
            ("folded", "one two"),
            ("empty", ""));
        card.Items[2].Source.Should().Match<MdSourceSpan>(span => span.StartLine == 3 && span.EndLine == 5);
        blocks.OfType<ParagraphVm>().Should().ContainSingle();
    }

    [Fact]
    public void Строка_front_matter_без_ключа_не_теряется()
    {
        var card = Parse("---\njust text\nkey: value\n---\n", new PropertiesCard()).OfType<PropertiesVm>().Single();

        card.Items.Select(item => (ModelDump.Text(item.Key), ModelDump.Text(item.Value))).Should().Equal(("", "just text"), ("key", "value"));
    }

    [Fact]
    public void Карточка_свойств_копируется_текстом_а_в_разметку_идёт_front_matter()
    {
        const string markdown = "---\ntitle: Hello\ntags: [a, b]\n---\n\nText";

        var blocks = Parse(markdown, new PropertiesCard());
        var map = MdTextMap.Build(blocks);

        map.Text.Should().StartWith("title\tHello\ntags\ta, b");
        MdMarkdownWriter.Write(blocks, map, markdown, new(0, map.Length)).Should().Be(markdown);
    }

    [Fact]
    public void Карточки_рисуются_шаблонами_темы_и_нажимаются_как_ссылки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "---\ntitle: Hello\n---\n\n![Talk](https://vimeo.com/123456)\n\n{{< pdf guide.pdf >}}" };
            viewer.Extensions.Add(new MdExtensionSet { Extensions = { new VideoCards(), new Shortcodes(), new PropertiesCard() } });
            var window = WpfHost.Show(viewer);
            var heard = new List<string>();
            window.AddHandler(MarkdownLink.NavigateEvent, new MarkdownLinkEventHandler((_, e) =>
            {
                heard.Add(e.Href);
                e.Handled = true;
            }));

            Visuals.Descendants<MdTextBlock>(viewer)
                .SelectMany(text => text.Runs?.OfType<RunVm>() ?? [])
                .Select(run => run.Text)
                .Should().Contain(["title", "Hello"]);
            foreach (var card in Visuals.Descendants<Button>(viewer).Where(button => button.Command == MarkdownLink.ActivateCommand).ToList())
            {
                ((IInvokeProvider)new ButtonAutomationPeer(card).GetPattern(PatternInterface.Invoke)).Invoke();
            }

            WpfHost.Settle(window);
            heard.Should().Equal("https://vimeo.com/123456", "guide.pdf");
            window.Close();
        });
    }

    [Fact]
    public void Шрифт_и_цвет_текста_карточек_берутся_у_приложения()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "---\ntitle: Hello\n---\n\n![Talk](https://vimeo.com/123456)\n\n{{< pdf docs/guide.pdf >}}" };
            viewer.Extensions.Add(new MdExtensionSet { Extensions = { new VideoCards(), new Shortcodes(), new PropertiesCard() } });
            var host = new Border { Child = viewer };
            TextElement.SetForeground(host, Brushes.Green);
            TextElement.SetFontFamily(host, new("Consolas"));
            var window = WpfHost.Show(host);

            var texts = Visuals.Descendants<TextBlock>(viewer).ToList();
            texts.Should().OnlyContain(text => text.FontFamily.Source == "Consolas");
            texts.Single(text => text.Text == "Talk").Foreground.Should().Be(Brushes.Green);
            Visuals.Descendants<MdTextBlock>(viewer)
                .Where(text => text.Runs?.OfType<RunVm>().Any(run => run.Text is "title" or "Hello") == true)
                .Should().HaveCount(2).And.OnlyContain(text => ReferenceEquals(text.Foreground, Brushes.Green));
            texts.Select(text => text.Text).Should().Contain(["guide.pdf", "docs/guide.pdf"]);
            window.Close();
        });
    }

    [Fact]
    public void Карточки_берут_общее_скругление_рамку_таблицы_и_вид_подписи()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "![Talk](https://vimeo.com/123456)\n\n{{< pdf docs/guide.pdf >}}" };
            viewer.Extensions.Add(new MdExtensionSet { Extensions = { new VideoCards(), new Shortcodes() } });
            viewer.Resources["Md.Card.CornerRadius"] = new CornerRadius(9);
            viewer.Resources["Md.Table.BorderThickness"] = new Thickness(3);
            viewer.Resources["Md.Figure.CaptionForeground"] = Brushes.Red;
            viewer.Resources["Md.Figure.CaptionFontSize"] = 20.0;
            var window = WpfHost.Show(viewer);

            Visuals.Descendants<Button>(viewer)
                .Select(card => Visuals.Descendants<Border>(card).First())
                .Should().HaveCount(2)
                .And.OnlyContain(frame => frame.CornerRadius == new CornerRadius(9) && frame.BorderThickness == new Thickness(3));
            var address = Visuals.Descendants<TextBlock>(viewer).Single(text => text.Text == "docs/guide.pdf");
            address.Foreground.Should().Be(Brushes.Red);
            address.FontSize.Should().Be(20);
            window.Close();
        });
    }

    private static IReadOnlyList<BlockVm> Parse(string markdown, MdExtension extension)
        => ModelBuilder.Parse(markdown, ModelDump.Pipeline(extension), [extension]);

    private static IReadOnlyList<InlineVm> Inlines(string markdown, MdExtension extension)
        => Parse(markdown, extension).OfType<ParagraphVm>().Single().Inlines;

    private static string Copy(string markdown, MdExtension extension)
    {
        var blocks = Parse(markdown, extension);
        var map = MdTextMap.Build(blocks);
        return MdMarkdownWriter.Write(blocks, map, markdown, new(0, map.Length));
    }
}
