namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using Editor;

/// <summary>Исходник в окне: блок той же высоты, что нарисованный, перенесённые строки начинаются после маркера.</summary>
public sealed class SourceLayoutTests
{
    [Theory]
    [InlineData("# Heading one")]
    [InlineData("## Heading two")]
    [InlineData("### Heading three")]
    [InlineData("#### Heading four")]
    [InlineData("##### Heading five")]
    [InlineData("###### Heading six")]
    [InlineData("A plain paragraph")]
    public void Блок_исходником_той_же_высоты_что_нарисованный(string block)
    {
        WpfHost.Invoke(() =>
        {
            var markdown = $"Intro\n\n{block}\n\nOutro";
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Blocks };
            var window = WpfHost.Show(new ScrollViewer { Content = editor });
            var drawn = ((FrameworkElement)editor.ItemContainerGenerator.ContainerFromIndex(2)).ActualHeight;

            editor.SourceCaretOffset = markdown.IndexOf(block, StringComparison.Ordinal) + 2;
            WpfHost.Settle(window);

            editor.WindowElement.ActualHeight.Should().BeApproximately(drawn, 2);
            window.Close();
        });
    }

    [Theory]
    [InlineData("- ", "an item")]
    [InlineData("- [ ] ", "a task")]
    [InlineData("1. ", "a numbered item")]
    [InlineData("> ", "a quote")]
    [InlineData("> - ", "an item in a quote")]
    public void Перенесённая_строка_начинается_после_маркера(string marker, string text)
    {
        WpfHost.Invoke(() =>
        {
            var line = marker + text + string.Concat(Enumerable.Repeat(" with long words", 30));
            var editor = new MarkdownEditor { Markdown = line, EditMode = MdEditMode.Blocks, Width = 300 };
            var window = WpfHost.Show(new ScrollViewer { Content = editor });

            editor.SourceCaretOffset = marker.Length + 1;
            WpfHost.Settle(window);

            var view = editor.TextEditor.TextArea.TextView;
            var visual = view.GetOrConstructVisualLine(editor.TextEditor.Document.GetLineByNumber(1));
            visual.TextLines.Should().HaveCountGreaterThan(1);
            var textStart = visual.GetTextLineVisualXPosition(visual.TextLines[0], visual.GetVisualColumn(marker.Length));
            var wrapped = visual.GetTextLineVisualXPosition(visual.TextLines[1], visual.GetTextLineVisualStartColumn(visual.TextLines[1]));
            wrapped.Should().BeApproximately(textStart, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Курсор_в_начале_строки_с_распоркой_встаёт_за_один_шаг()
    {
        WpfHost.Invoke(() =>
        {
            const string markdown = "Intro\n\n# Heading\n\nOutro";
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Blocks };
            var window = WpfHost.Show(new ScrollViewer { Content = editor });
            var start = markdown.IndexOf('#', StringComparison.Ordinal);
            editor.SourceCaretOffset = start;
            WpfHost.Settle(window);

            System.Windows.Documents.EditingCommands.MoveRightByCharacter.Execute(null, editor.TextEditor.TextArea);

            editor.SourceCaretOffset.Should().Be(start + 1);
            window.Close();
        });
    }
}
