namespace MarkdigWpfViewer.Tests;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using Presenters;

/// <summary>Нажатие ссылок: приложение, якоря документа, открыватель; оглавление и подсказки.</summary>
public sealed class LinkTests
{
    [Fact]
    public void Необработанную_ссылку_открывает_открыватель()
    {
        WpfHost.Invoke(() =>
        {
            var opener = new RecordingOpener();
            var viewer = new MarkdownViewer { Markdown = "[site](https://example.com/page)" };
            MarkdownLink.SetOpener(viewer, opener);
            var window = WpfHost.Show(viewer);

            Click(LinkOf(viewer, "site"));

            opener.Opened.Should().ContainSingle().Which.Should().Be("https://example.com/page");
            window.Close();
        });
    }

    [Fact]
    public void Обработанную_приложением_ссылку_открыватель_не_получает()
    {
        WpfHost.Invoke(() =>
        {
            var opener = new RecordingOpener();
            var viewer = new MarkdownViewer { Markdown = "[mine](app:42)" };
            var host = new Border { Child = viewer };
            MarkdownLink.SetOpener(host, opener);
            MarkdownLink.AddNavigateHandler(host, (_, e) => e.Handled = e.Href == "app:42");
            var window = WpfHost.Show(host);

            Click(LinkOf(viewer, "mine"));

            opener.Opened.Should().BeEmpty();
            window.Close();
        });
    }

    [Fact]
    public void Открыватель_наследуется_от_предка_а_без_него_ссылка_не_открывается()
    {
        WpfHost.Invoke(() =>
        {
            var opener = new RecordingOpener();
            var viewer = new MarkdownViewer { Markdown = "[a](https://a.example) [b](https://b.example)" };
            var host = new Border { Child = viewer };
            MarkdownLink.SetOpener(host, opener);
            var window = WpfHost.Show(host);

            Click(LinkOf(viewer, "a"));
            MarkdownLink.SetOpener(viewer, null);
            Click(LinkOf(viewer, "b"));

            opener.Opened.Should().Equal("https://a.example");
            window.Close();
        });
    }

    [Fact]
    public void Ссылка_на_якорь_прокручивает_к_заголовку_мимо_открывателя()
    {
        WpfHost.Invoke(() =>
        {
            var filler = string.Join("\n\n", Enumerable.Range(0, 100).Select(i => $"Paragraph {i}"));
            var opener = new RecordingOpener();
            var viewer = new MarkdownViewer { Markdown = $"[go](#target-heading)\n\n{filler}\n\n## Target heading\n\n{filler}" };
            MarkdownLink.SetOpener(viewer, opener);
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);

            Click(LinkOf(viewer, "go"));
            WpfHost.Settle(window);

            viewer.TextMap.TryGetAnchor("target-heading", out var heading).Should().BeTrue();
            var rect = viewer.GetRangeRects(heading).Should().ContainSingle().Subject;
            viewer.TranslatePoint(rect.TopLeft, scroll).Y.Should().BeApproximately(0, 1);
            opener.Opened.Should().BeEmpty();
            window.Close();
        });
    }

    [Fact]
    public void Незнакомый_якорь_уходит_в_открыватель()
    {
        WpfHost.Invoke(() =>
        {
            var opener = new RecordingOpener();
            var viewer = new MarkdownViewer { Markdown = "[x](#nowhere)" };
            MarkdownLink.SetOpener(viewer, opener);
            var window = WpfHost.Show(viewer);

            Click(LinkOf(viewer, "x"));

            opener.Opened.Should().Equal("#nowhere");
            window.Close();
        });
    }

    [Fact]
    public void Якоря_заголовков_как_у_GitHub_и_явные()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "# Hello World\n\n## Привет, мир!\n\n# Hello World\n\n### Своё имя {#custom}" };
            var window = WpfHost.Show(viewer);

            viewer.TextMap.Headings.Select(heading => heading.Id).Should().Equal("hello-world", "привет-мир", "hello-world-1", "своё-имя");
            viewer.TextMap.TryGetAnchor("custom", out var custom).Should().BeTrue();
            custom.Should().Be(viewer.TextMap.Headings[3].Range);
            window.Close();
        });
    }

    [Fact]
    public void Оглавление_знает_уровни_тексты_и_строки_заголовков()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "# Top\n\ntext\n\n## **Bold** part\n\n> ### In quote" };
            var window = WpfHost.Show(viewer);

            viewer.TextMap.Headings.Select(heading => (heading.Level, heading.Text, heading.SourceLine))
                .Should().Equal((1, "Top", 0), (2, "Bold part", 4), (3, "In quote", 6));
            window.Close();
        });
    }

    [Fact]
    public void Подсказка_ссылки_её_заголовок_а_без_него_адрес()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "[a](https://a.example \"Title A\") [b](https://b.example)" };
            var window = WpfHost.Show(viewer);

            LinkOf(viewer, "a").ToolTip.Should().Be("Title A");
            LinkOf(viewer, "b").ToolTip.Should().Be("https://b.example");
            window.Close();
        });
    }

    private static Hyperlink LinkOf(MarkdownViewer viewer, string text)
        => Visuals.Descendants<MdTextBlock>(viewer)
            .SelectMany(block => Hyperlinks(block.Inlines))
            .First(link => TextOf(link) == text);

    private static string TextOf(Inline inline)
        => inline switch
        {
            Run run => run.Text,
            Span span => string.Concat(span.Inlines.Select(TextOf)),
            _ => string.Empty
        };

    private static IEnumerable<Hyperlink> Hyperlinks(InlineCollection inlines)
        => inlines.SelectMany(inline => inline switch
        {
            Hyperlink link => [link],
            Span span => Hyperlinks(span.Inlines),
            _ => Enumerable.Empty<Hyperlink>()
        });

    private static void Click(Hyperlink link)
        => link.RaiseEvent(new RequestNavigateEventArgs(link.NavigateUri, null) { RoutedEvent = Hyperlink.RequestNavigateEvent });

    private sealed class RecordingOpener : IMdLinkOpener
    {
        public List<string> Opened { get; } = [];

        public void Open(string href, Uri? uri) => Opened.Add(href);
    }
}