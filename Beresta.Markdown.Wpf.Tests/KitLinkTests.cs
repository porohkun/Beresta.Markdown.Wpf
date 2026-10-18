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

/// <summary>Готовые расширения связей: вики-ссылки, встраивания, теги, упоминания, задачи, оглавление.</summary>
public sealed class KitLinkTests
{
    [Theory]
    [InlineData("[[Page]]", "Page", "wiki:Page")]
    [InlineData("[[Page#Section]]", "Page › Section", "wiki:Page#Section")]
    [InlineData("[[Page|label]]", "label", "wiki:Page")]
    [InlineData("[[#Some Heading]]", "Some Heading", "#some-heading")]
    [InlineData("[[My Page]]", "My Page", "wiki:My Page")]
    public void Вики_ссылка_становится_ссылкой_wiki(string markdown, string text, string href)
    {
        var run = Runs(markdown, new WikiLinks()).Single();

        (run.Text, run.Href, run.StyleName).Should().Be((text, href, "WikiLink"));
        run.Hyperlink.Should().NotBeNull();
    }

    [Fact]
    public void Страница_которой_нет_рисуется_своим_оформлением()
    {
        var links = new WikiLinks { Resolver = new Resolver() };

        Runs("[[Known]] [[Absent]]", links).Where(run => run.Href is not null).Select(run => run.StyleName)
            .Should().Equal("WikiLink", "WikiLink.Missing");
    }

    [Theory]
    [InlineData("[[_TOC_]]")]
    [InlineData("[[toc]]")]
    [InlineData("[[]]")]
    public void Не_вики_ссылка_остаётся_текстом(string markdown)
    {
        Runs(markdown, new WikiLinks()).Should().NotContain(run => run.Href != null);
    }

    [Fact]
    public void Ссылка_внутри_жирного_остаётся_жирной()
    {
        Runs("**see [[Page]]**", new WikiLinks()).Single(run => run.Href is not null).Bold.Should().BeTrue();
    }

    [Theory]
    [InlineData("![[img.png]]", "img.png", "img.png", null, null)]
    [InlineData("![[pics/img.png|200]]", "pics/img.png", "img.png", 200.0, null)]
    [InlineData("![[img.png|200x100]]", "img.png", "img.png", 200.0, 100.0)]
    [InlineData("![[img.png|An image]]", "img.png", "An image", null, null)]
    public void Встраивание_картинки_становится_картинкой(string markdown, string url, string alt, double? width, double? height)
    {
        var image = Parse(markdown, new WikiEmbeds()).OfType<ParagraphVm>().Single().Inlines.OfType<ImageVm>().Single();

        (image.Url, image.Alt, image.Width, image.Height).Should().Be((url, alt, width, height));
    }

    [Fact]
    public void Встраивание_не_картинки_становится_вики_ссылкой()
    {
        Runs("![[Note]]", new WikiEmbeds()).Single().Href.Should().Be("wiki:Note");
    }

    [Theory]
    [InlineData("see #tag here", "#tag", "tag:tag")]
    [InlineData("#tag/nested", "#tag/nested", "tag:tag/nested")]
    [InlineData("**#bold**", "#bold", "tag:bold")]
    public void Тег_становится_чипом_со_ссылкой(string markdown, string text, string href)
    {
        var run = Runs(markdown, new Tags()).Single(run => run.StyleName == "Tag");

        (run.Text, run.Href).Should().Be((text, href));
    }

    [Theory]
    [InlineData("C# and F#")]
    [InlineData("issue #123")]
    [InlineData("[link #tag](https://x.example)")]
    [InlineData("`#code`")]
    [InlineData("&#35;entity")]
    public void Не_тег_остаётся_текстом(string markdown)
    {
        Runs(markdown, new Tags()).Should().NotContain(run => run.StyleName == "Tag");
    }

    [Theory]
    [InlineData("hi @ivan!", "ivan")]
    [InlineData("Thanks @ivan.", "ivan")]
    [InlineData("@org/team please", "org/team")]
    public void Упоминание_становится_чипом(string markdown, string name)
    {
        Inlines(markdown, new Mentions()).OfType<MentionVm>().Single().Name.Should().Be(name);
    }

    [Theory]
    [InlineData("mail a@b.com")]
    [InlineData("[@ivan](https://x.example)")]
    public void Не_упоминание_остаётся_текстом(string markdown)
    {
        Inlines(markdown, new Mentions()).OfType<MentionVm>().Should().BeEmpty();
    }

    [Fact]
    public void Имя_упоминания_даёт_резолвер_а_в_разметку_идёт_исходник()
    {
        const string markdown = "hi @ivan!";
        var mentions = new Mentions { Resolver = new Resolver() };
        var blocks = Parse(markdown, mentions);
        var map = MdTextMap.Build(blocks);

        blocks.OfType<ParagraphVm>().Single().Inlines.OfType<MentionVm>().Single().DisplayName.Should().Be("Ivan Petrov");
        map.GetText(new(0, map.Length)).Should().Be("hi @Ivan Petrov!");
        MdMarkdownWriter.Write(blocks, map, markdown, new(0, map.Length)).Should().Be(markdown);
    }

    [Fact]
    public void Задачи_становятся_ссылками_по_шаблонам()
    {
        var issues = Issues();
        var links = Runs("Fixed #12, PROJ-7 and org/repo#45; not C#1, abc#3 or XPROJ-7x.", issues)
            .Where(run => run.Href is not null)
            .Select(run => (run.Text, run.Href))
            .ToList();

        links.Should().Equal(
            ("#12", "https://x.example/issues/12"),
            ("PROJ-7", "https://jira.example/browse/PROJ-7"),
            ("org/repo#45", "https://github.com/org/repo/issues/45"));
    }

    [Fact]
    public void Без_шаблонов_задач_нет()
    {
        Runs("Fixed #12", new IssueLinks()).Should().NotContain(run => run.Href != null);
    }

    [Fact]
    public void Задача_в_жирном_жирная_а_в_ссылке_не_задача()
    {
        Runs("**see #12**", Issues()).Single(run => run.Href is not null).Bold.Should().BeTrue();
        Runs("[see #12](https://y.example)", Issues()).Select(run => run.Href).Should().AllBe("https://y.example");
    }

    [Fact]
    public void Задача_и_тег_живут_рядом()
    {
        MdExtension[] extensions = [new Tags(), Issues()];
        var runs = ModelBuilder.Parse("#12 and #tag", ModelDump.Pipeline(extensions), extensions)
            .OfType<ParagraphVm>().Single().Inlines.OfType<RunVm>().ToList();

        runs.Single(run => run.Text == "#12").Href.Should().Be("https://x.example/issues/12");
        runs.Single(run => run.Text == "#tag").StyleName.Should().Be("Tag");
    }

    [Theory]
    [InlineData("see [[Page]] and [[Page|label]] or [[#Head]]", "see [[Page]] and [[Page|label]] or [[#Head|Head]]")]
    [InlineData("![[img.png|200]] and #tag", "![[img.png|200]] and #tag")]
    [InlineData("Fixed #12 and PROJ-7 today", "Fixed #12 and PROJ-7 today")]
    public void Связи_пишутся_в_разметку_своим_синтаксисом(string markdown, string expected)
    {
        MdExtension[] extensions = [new WikiLinks(), new WikiEmbeds(), new Tags(), Issues()];
        var blocks = ModelBuilder.Parse(markdown, ModelDump.Pipeline(extensions), extensions);
        var map = MdTextMap.Build(blocks);

        MdMarkdownWriter.Write(blocks, map, markdown, new(0, map.Length)).Should().Be(expected);
    }

    [Theory]
    [InlineData("[TOC]")]
    [InlineData("[[_TOC_]]")]
    [InlineData("[[toc]]")]
    public void Абзац_оглавления_становится_оглавлением(string marker)
    {
        MdExtension[] extensions = [new TableOfContents(), new WikiLinks()];

        ModelBuilder.Parse($"# A\n\n{marker}\n\n## B", ModelDump.Pipeline(extensions), extensions).OfType<TocVm>().Should().ContainSingle();
    }

    [Fact]
    public void Оглавление_следует_за_текстом_и_ведёт_к_заголовку()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "[TOC]\n\n# First\n\n## Second" };
            viewer.Extensions.Add(new TableOfContents());
            var window = WpfHost.Show(viewer);
            var heard = new List<string>();
            window.AddHandler(MarkdownLink.NavigateEvent, new MarkdownLinkEventHandler((_, e) => heard.Add(e.Href)));

            Links(viewer).Select(link => ((Run)link.Inlines.FirstInline).Text).Should().Equal("First", "Second");
            var second = Links(viewer)[1];
            ((System.Windows.Input.RoutedCommand)second.Command).Execute(second.CommandParameter, second);
            heard.Should().Contain("#second");

            viewer.Markdown += "\n\n### Third";
            WpfHost.Settle(window);
            Links(viewer).Should().HaveCount(3);
            window.Close();
        });
    }

    [Fact]
    public void Упоминание_нажимается_как_ссылка()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "hi @ivan" };
            viewer.Extensions.Add(new Mentions());
            var window = WpfHost.Show(viewer);
            var heard = new List<string>();
            window.AddHandler(MarkdownLink.NavigateEvent, new MarkdownLinkEventHandler((_, e) =>
            {
                heard.Add(e.Href);
                e.Handled = true;
            }));
            var chip = Visuals.Descendants<Button>(viewer).Single();

            ((IInvokeProvider)new ButtonAutomationPeer(chip).GetPattern(PatternInterface.Invoke)).Invoke();
            WpfHost.Settle(window);

            heard.Should().Equal("mention:ivan");
            window.Close();
        });
    }

    [Fact]
    public void Тег_и_упоминание_берут_фон_упоминания_и_цвет_ссылки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "#idea and @ivan" };
            viewer.Extensions.Add(new Tags());
            viewer.Extensions.Add(new Mentions());
            viewer.Resources["Md.Mention.Background"] = Brushes.Yellow;
            viewer.Resources["Md.Paragraph.LinkForeground"] = Brushes.Red;
            var window = WpfHost.Show(viewer);

            var style = (Style)viewer.FindResource("Md.Tag.Style");
            var tag = Visuals.Descendants<MdTextBlock>(viewer)
                .SelectMany(text => Spans(text.Inlines))
                .Single(span => ReferenceEquals(span.Style, style));
            tag.Background.Should().Be(Brushes.Yellow);
            tag.Foreground.Should().Be(Brushes.Red);
            var chip = Visuals.Descendants<Button>(viewer).Single();
            Visuals.Descendants<Border>(chip).First().Background.Should().Be(Brushes.Yellow);
            Visuals.Descendants<TextBlock>(chip).Single().Foreground.Should().Be(Brushes.Red);
            window.Close();
        });
    }

    private static IssueLinks Issues() => new()
    {
        Templates =
        {
            ["#"] = "https://x.example/issues/{0}",
            ["PROJ-"] = "https://jira.example/browse/PROJ-{0}",
            ["org/repo#"] = "https://github.com/org/repo/issues/{0}"
        }
    };

    private static IReadOnlyList<BlockVm> Parse(string markdown, MdExtension extension)
        => ModelBuilder.Parse(markdown, ModelDump.Pipeline(extension), [extension]);

    private static IReadOnlyList<InlineVm> Inlines(string markdown, MdExtension extension)
        => Parse(markdown, extension).OfType<ParagraphVm>().Single().Inlines;

    private static List<RunVm> Runs(string markdown, MdExtension extension)
        => Inlines(markdown, extension).OfType<RunVm>().ToList();

    private static IEnumerable<Span> Spans(InlineCollection inlines)
        => inlines.OfType<Span>().SelectMany(span => Spans(span.Inlines).Prepend(span));

    private static List<Hyperlink> Links(MarkdownViewer viewer)
        => Visuals.Descendants<MdListPresenter>(viewer)
            .SelectMany(Visuals.Descendants<TextBlock>)
            .SelectMany(text => text.Inlines.OfType<Hyperlink>())
            .ToList();

    private sealed class Resolver : IMdLinkResolver
    {
        public bool PageExists(string page) => page == "Known";

        public string? DisplayName(string user) => user == "ivan" ? "Ivan Petrov" : null;
    }
}
