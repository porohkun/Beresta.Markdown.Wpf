namespace MarkdigWpfViewer.Tests;

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Presenters;
using Selection;

/// <summary>Привычное поведение текста: щелчки, меню, курсор, копирование кода и HTML в буфере.</summary>
public sealed partial class BehaviorTests
{
    private const string ThreeParagraphs = "first paragraph\n\nsecond **bold** paragraph\n\nthird paragraph";

    [Fact]
    public void Двойной_щелчок_выделяет_слово()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            var text = viewer.TextMap.Text;

            viewer.Selection.SelectWordAt(Visuals.PointOf(viewer, text.IndexOf("bold", StringComparison.Ordinal) + 2)).Should().BeTrue();
            viewer.SelectedText.Should().Be("bold");

            viewer.Selection.SelectWordAt(Visuals.PointOf(viewer, text.IndexOf("third", StringComparison.Ordinal) + 4));
            viewer.SelectedText.Should().Be("third");
            window.Close();
        });
    }

    [Fact]
    public void Тройной_щелчок_выделяет_текст_блока()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);

            viewer.Selection.SelectBlockAt(Visuals.PointOf(viewer, viewer.TextMap.Text.IndexOf("bold", StringComparison.Ordinal))).Should().BeTrue();

            viewer.SelectedText.Should().Be("second bold paragraph");
            window.Close();
        });
    }

    [Fact]
    public void Щелчки_при_выключенном_выделении_не_выделяют()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs, IsSelectionEnabled = false };
            var window = WpfHost.Show(viewer);
            var point = Visuals.PointOf(viewer, 2);

            viewer.Selection.SelectWordAt(point).Should().BeFalse();
            viewer.Selection.SelectBlockAt(point).Should().BeFalse();
            viewer.SelectionLength.Should().Be(0);
            window.Close();
        });
    }

    [Fact]
    public void Меню_по_умолчанию_копирует_копирует_разметкой_и_выделяет_всё()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);

            var items = viewer.ContextMenu.Should().NotBeNull().And.Subject.As<ContextMenu>().Items.OfType<MenuItem>().ToList();
            items.Select(item => item.Command).Should().Equal(ApplicationCommands.Copy, MarkdownViewer.CopyMarkdownCommand, ApplicationCommands.SelectAll);
            items.Select(item => item.Header).Should().Equal("Copy", "Copy as Markdown", "Select All");
            window.Close();
        });
    }

    [Fact]
    public void Копирование_как_markdown_кладёт_разметку_обычным_текстом()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            viewer.SelectAll();

            MarkdownViewer.CopyMarkdownCommand.InputGestures.OfType<KeyGesture>()
                .Should().ContainSingle(gesture => gesture.Key == Key.C && gesture.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift));
            MarkdownViewer.CopyMarkdownCommand.CanExecute(null, viewer).Should().BeTrue();
            viewer.CreateCopyData(markdownAsText: true)!.GetText().Should().Be(ThreeParagraphs);
            window.Close();
        });
    }

    [Fact]
    public void В_буфер_идёт_и_HTML_с_точными_позициями_фрагмента()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "Привет, **мир**" };
            var window = WpfHost.Show(viewer);
            viewer.SelectAll();

            var html = viewer.CreateCopyData()!.GetData(DataFormats.Html).Should().BeOfType<string>().Subject;

            var bytes = Encoding.UTF8.GetBytes(html);
            var start = int.Parse(Offset().Match(html).Groups["start"].Value);
            var end = int.Parse(Offset().Match(html).Groups["end"].Value);
            Encoding.UTF8.GetString(bytes, start, end - start).Should().Be("<p>Привет, <strong>мир</strong></p>\n");
            window.Close();
        });
    }

    [Fact]
    public void Над_текстом_текстовый_курсор_пока_выделение_включено()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "plain text" };
            var window = WpfHost.Show(viewer);
            var run = Visuals.Descendants<MdTextBlock>(viewer).Single().Inlines.OfType<Run>().Single();

            QueryCursor(run).Should().Be(Cursors.IBeam);

            viewer.IsSelectionEnabled = false;
            QueryCursor(run).Should().BeNull();
            window.Close();
        });
    }

    [Fact]
    public void Кнопка_кода_копирует_код_через_Copying()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "```cs\nvar x = 1;\n```" };
            var copying = 0;
            DataObject.AddCopyingHandler(viewer, (_, _) => copying++);
            var window = WpfHost.Show(viewer);
            var code = Visuals.Descendants<CodeBlockPresenter>(viewer).Single();

            var data = code.CreateCopyData()!;

            data.GetText().Should().Be("var x = 1;");
            var markdown = data.GetData(MdDataFormats.Markdown).Should().BeOfType<MemoryStream>().Subject;
            Encoding.UTF8.GetString(markdown.ToArray()).Should().Be("```cs\nvar x = 1;\n```");
            data.GetData(DataFormats.Html).Should().BeOfType<string>()
                .Which.Should().Contain("<pre><code class=\"language-cs\">var x = 1;</code></pre>");
            copying.Should().Be(1);
            code.Template.FindName("PART_Copy", code).Should().BeOfType<Button>().Which.Visibility.Should().Be(Visibility.Collapsed);
            window.Close();
        });
    }

    private static Cursor? QueryCursor(ContentElement element)
    {
        var args = new QueryCursorEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.QueryCursorEvent };
        element.RaiseEvent(args);
        return args.Handled ? args.Cursor : null;
    }

    [GeneratedRegex(@"StartFragment:(?<start>\d+)\r\nEndFragment:(?<end>\d+)")]
    private static partial Regex Offset();
}