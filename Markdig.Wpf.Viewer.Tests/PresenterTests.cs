namespace MarkdigWpfViewer.Tests;

using System.Windows.Documents;
using System.Windows.Media;
using Presenters;
using ViewModels;

/// <summary>Презентеры блоков: шрифт текста и заголовки.</summary>
public sealed class PresenterTests
{
    [Fact]
    public void Текст_следует_за_шрифтом_блока()
    {
        WpfHost.Invoke(() =>
        {
            var block = new MdTextBlock
            {
                FontSize = 14,
                Runs = [new RunVm("text"), new RunVm("link", Hyperlink: new Uri("https://example.com"))]
            };

            block.FontSize = 28;
            block.FontFamily = new FontFamily("Consolas");

            var runs = RunsOf(block.Inlines).ToList();
            runs.Should().HaveCount(2);
            runs.Should().AllSatisfy(run =>
            {
                run.FontSize.Should().Be(28);
                run.FontFamily.Source.Should().Be("Consolas");
            });
        });
    }

    [Fact]
    public void Заголовок_рисуется_размером_своего_уровня()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "# One\n\n## Two" };
            var window = WpfHost.Show(viewer);

            var headings = FindAll<MdHeadingBlock>(viewer).ToList();
            headings.Select(heading => heading.Level).Should().Equal(1, 2);
            headings.Select(heading => heading.FontSize).Should().Equal(28, 24);
            window.Close();
        });
    }

    private static IEnumerable<Run> RunsOf(IEnumerable<Inline> inlines)
    {
        return inlines.SelectMany<Inline, Run>(inline => inline switch
        {
            Run run => [run],
            Span span => RunsOf(span.Inlines),
            _ => []
        });
    }

    private static IEnumerable<T> FindAll<T>(System.Windows.DependencyObject root)
        where T : System.Windows.DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T found)
            {
                yield return found;
            }

            foreach (var deeper in FindAll<T>(child))
            {
                yield return deeper;
            }
        }
    }
}
