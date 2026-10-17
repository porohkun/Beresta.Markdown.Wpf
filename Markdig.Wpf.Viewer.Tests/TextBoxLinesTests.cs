namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Controls;
using Scrolling;

/// <summary>Строки исходника в редакторе: место строки, видимая строка, прокрутка к строке, синхронная прокрутка с вьюером.</summary>
public sealed class TextBoxLinesTests
{
    private static readonly string LongText = string.Join("\n\n", Enumerable.Range(0, 300).Select(i => $"Paragraph {i}"));

    [Fact]
    public void Строка_считается_по_знакам_и_перенос_её_не_сбивает()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new TextBox
            {
                Text = "short\n" + string.Join(" ", Enumerable.Repeat("word", 80)) + "\nafter",
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true
            };
            var window = WpfHost.Show(editor);
            var lines = new MdTextBoxLines(editor);

            var after = editor.GetRectFromCharacterIndex(editor.Text.IndexOf("after", StringComparison.Ordinal)).Top;
            editor.LineCount.Should().BeGreaterThan(3);
            lines.GetSourceLineOffset(2).Should().BeApproximately(after, 0.5);
            lines.VisibleRows(editor).Select(row => row.Line).Should().Equal(0, 1, 2);
            lines.VisibleRows(editor)[2].Top.Should().BeApproximately(after, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Редактор_прокручивается_к_строке_и_называет_видимую()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new TextBox { Text = LongText, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var window = WpfHost.Show(editor, height: 300);
            var lines = new MdTextBoxLines(editor);

            lines.ScrollToSourceLine(300, 0.38).Should().BeTrue();
            WpfHost.Settle(window);

            lines.GetVisibleSourceLine(0.38).Should().BeApproximately(300, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Вьюер_догоняет_прокрученный_редактор()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new TextBox { Text = LongText, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var viewer = new MarkdownViewer { Markdown = LongText };
            var viewerScroll = new ScrollViewer { Content = viewer };
            Grid.SetColumn(viewerScroll, 1);
            var grid = new Grid { ColumnDefinitions = { new(), new() }, Children = { editor, viewerScroll } };
            var window = WpfHost.Show(grid);
            var lines = new MdTextBoxLines(editor);
            using var sync = new MdScrollSync(lines, viewer, MdLineMap.Identity);

            lines.Scroller!.ScrollToVerticalOffset(3000);
            WpfHost.Settle(window);

            viewer.GetVisibleSourceLine(0.38).Should().BeApproximately(lines.GetVisibleSourceLine(0.38)!.Value, 1);
            window.Close();
        });
    }
}
