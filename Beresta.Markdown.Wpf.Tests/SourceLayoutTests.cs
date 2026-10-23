namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using Editor;

/// <summary>
/// Исходник в окне: блок той же высоты, что нарисованный, пустая строка — высотой отступа, перенесённые
/// строки начинаются после маркера; шаг курсора и протяжка не сдвигают строк вокруг окна.
/// </summary>
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
    [InlineData("> [!WARNING]\n> A callout body")]
    [InlineData("> [!NOTE]\n> First line of the body\n> Second line of the body")]
    [InlineData("> [!NOTE]\n> First paragraph\n>\n> Second paragraph")]
    [InlineData("```\nvar x = 1;\n```")]
    [InlineData("```csharp\nvar x = 1;\nvar y = 2;\n```")]
    [InlineData("    indented code")]
    [InlineData("| A | B |\n|---|---|\n| 1 | 2 |")]
    [InlineData("| Head | Other |\n|---|---|\n| one | two |\n| three | four |")]
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
    [InlineData("> First paragraph\n>\n> Second paragraph")]
    [InlineData("> First paragraph\n> \n> Second paragraph")]
    [InlineData("> > First paragraph\n> >\n> > Second paragraph")]
    public void Цитата_с_пустой_строкой_исходником_той_же_высоты(string block)
    {
        WpfHost.Invoke(() =>
        {
            var markdown = $"Intro\n\n{block}\n\nOutro";
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Blocks };
            var window = WpfHost.Show(new ScrollViewer { Content = editor });
            var drawn = ((FrameworkElement)editor.ItemContainerGenerator.ContainerFromIndex(2)).ActualHeight;

            editor.SelectSource(markdown.IndexOf(block, StringComparison.Ordinal), block.Length);
            WpfHost.Settle(window);

            editor.WindowLines.Should().Be((3, 5));
            ((FrameworkElement)editor.ItemContainerGenerator.ContainerFromIndex(2)).ActualHeight.Should().BeApproximately(drawn, 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Плашка_со_значком_выше_текста_исходником_той_же_высоты()
    {
        WpfHost.Invoke(() =>
        {
            const string markdown = "Intro\n\n> [!WARNING]\n> A callout body\n\nOutro";
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Blocks };
            editor.Resources["Md.Icon.Size"] = 40.0;
            var window = WpfHost.Show(new ScrollViewer { Content = editor });
            var drawn = ((FrameworkElement)editor.ItemContainerGenerator.ContainerFromIndex(2)).ActualHeight;

            editor.SourceCaretOffset = markdown.IndexOf('>', StringComparison.Ordinal) + 2;
            WpfHost.Settle(window);

            editor.WindowElement.ActualHeight.Should().BeApproximately(drawn, 0.5);
            window.Close();
        });
    }

    [Theory]
    [InlineData("# Heading one")]
    [InlineData("## Heading two")]
    public void Окно_на_заголовке_оставляет_его_текст_на_месте(string heading)
    {
        WpfHost.Invoke(() =>
        {
            var around = string.Join("\n\n", Enumerable.Range(0, 12).Select(i => $"Paragraph {i}"));
            var markdown = $"{around}\n\n{heading}\n\n{around}";
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Blocks };
            var scroller = new ScrollViewer { Content = editor };
            var window = WpfHost.Show(scroller);
            scroller.ScrollToVerticalOffset(150);
            WpfHost.Settle(window);
            var line = around.Count(symbol => symbol == '\n') + 2;
            var drawn = editor.TranslatePoint(new(0, editor.GetSourceLineOffset(line)!.Value), scroller).Y;

            editor.SourceCaretOffset = markdown.IndexOf(heading, StringComparison.Ordinal) + 3;
            WpfHost.Settle(window);

            editor.WindowLines.Should().NotBeNull();
            scroller.VerticalOffset.Should().BeApproximately(150, 0.5);
            editor.TranslatePoint(new(0, editor.GetSourceLineOffset(line)!.Value), scroller).Y.Should().BeApproximately(drawn, 1);
            window.Close();
        });
    }

    [Theory]
    [InlineData("## Heading two", -1, true)]
    [InlineData("> [!WARNING]\n> A callout body", -1, true)]
    [InlineData("> [!WARNING]\n> A callout body", 1, false)]
    [InlineData("- [ ] one\n- [ ] two\n- [ ] three", 0, true)]
    [InlineData("```\nvar x = 1;\n```", -1, true)]
    [InlineData("| A | B |\n|---|---|\n| 1 | 2 |", -1, true)]
    public void Шаг_курсора_оставляет_строки_вокруг_на_месте(string block, int from, bool down)
    {
        WpfHost.Invoke(() =>
        {
            var around = string.Join("\n\n", Enumerable.Range(0, 12).Select(i => $"Paragraph {i}"));
            var markdown = $"{around}\n\n{block}\n\n{around}";
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Blocks };
            var scroller = new ScrollViewer { Content = editor };
            var window = WpfHost.Show(scroller);
            scroller.ScrollToVerticalOffset(150);
            WpfHost.Settle(window);
            var first = around.Count(symbol => symbol == '\n') + 2;
            var last = first + block.Count(symbol => symbol == '\n');
            int[] steady = [first - 2, last + 2];
            editor.SourceCaretOffset = editor.TextEditor.Document.GetLineByNumber(first + from + 1).Offset;
            WpfHost.Settle(window);
            var before = steady.Select(Top).ToList();

            (down ? System.Windows.Documents.EditingCommands.MoveDownByLine : System.Windows.Documents.EditingCommands.MoveUpByLine)
                .Execute(null, editor.TextEditor.TextArea);
            WpfHost.Settle(window);

            editor.TextEditor.TextArea.Caret.Line.Should().Be(first + from + (down ? 2 : 0));
            steady.Select(Top).Should().Equal(before, (after, was) => Math.Abs(after - was) < 0.5);
            window.Close();

            double Top(int line) => editor.TranslatePoint(new(0, editor.GetSourceLineOffset(line)!.Value), scroller).Y;
        });
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(6, 3)]
    [InlineData(1, 4)]
    [InlineData(6, 4)]
    public void Протяжка_встаёт_на_строку_под_мышью(int start, int target)
    {
        WpfHost.Invoke(() =>
        {
            const string markdown = "Intro\n\n> [!WARNING]\n> A callout body\n\nOutro";
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Blocks };
            var window = WpfHost.Show(new ScrollViewer { Content = editor });
            editor.SourceCaretOffset = editor.TextEditor.Document.GetLineByNumber(start).Offset;
            WpfHost.Settle(window);
            var point = new Point(40, editor.GetSourceLineOffset(target - 1)!.Value + 4);

            for (var step = 0; step < 3; step++)
            {
                editor.ExtendSelectionTo(point);
                WpfHost.Settle(window);
                editor.TextEditor.TextArea.Caret.Line.Should().Be(target);
            }

            window.Close();
        });
    }

    [Theory]
    [InlineData("First\n\nSecond", 2)]
    [InlineData("First\n   \nSecond", 2)]
    [InlineData("First\n\n\nSecond", 3)]
    [InlineData("- one\n\n- two", 2)]
    public void Пустая_строка_исходником_высотой_отступа_в_отрисовке(string markdown, int line)
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Full };
            var window = WpfHost.Show(new ScrollViewer { Content = editor });
            WpfHost.Settle(window);

            var visual = editor.TextEditor.TextArea.TextView.GetOrConstructVisualLine(editor.TextEditor.Document.GetLineByNumber(line));
            visual.Height.Should().BeApproximately((double)editor.FindResource("Md.BlankLine.Height"), 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Каретка_на_пустой_строке_посередине_её_высоты()
    {
        WpfHost.Invoke(() =>
        {
            const string markdown = "First\n\nSecond";
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Full };
            var window = WpfHost.Show(new ScrollViewer { Content = editor });
            editor.SourceCaretOffset = markdown.IndexOf('\n', StringComparison.Ordinal) + 1;
            WpfHost.Settle(window);

            var area = editor.TextEditor.TextArea;
            var caret = area.Caret.CalculateCaretRectangle();
            var visual = area.TextView.GetOrConstructVisualLine(editor.TextEditor.Document.GetLineByNumber(2));
            ((caret.Top + caret.Bottom) / 2).Should().BeApproximately(visual.VisualTop + (visual.Height / 2), 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Поля_перенесённой_строки_над_первым_рядом_и_под_последним()
    {
        WpfHost.Invoke(() =>
        {
            var body = string.Concat(Enumerable.Repeat("long words in the body ", 20));
            var markdown = $"Intro\n\n> [!NOTE]\n> {body}\n\nOutro";
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Blocks, Width = 400 };
            var window = WpfHost.Show(new ScrollViewer { Content = editor });
            editor.SourceCaretOffset = markdown.IndexOf("[!NOTE]", StringComparison.Ordinal);
            WpfHost.Settle(window);

            var rows = editor.TextEditor.TextArea.TextView.GetOrConstructVisualLine(editor.TextEditor.Document.GetLineByNumber(4)).TextLines;
            var top = ((Thickness)editor.FindResource("Md.Callout.ContentMargin")).Top;
            var bottom = ((Thickness)editor.FindResource("Md.Callout.Margin")).Bottom + ((Thickness)editor.FindResource("Md.Callout.Padding")).Bottom;
            rows.Should().HaveCountGreaterThan(2);
            rows[0].Height.Should().BeApproximately(rows[1].Height + top, 0.5);
            rows[^1].Height.Should().BeApproximately(rows[1].Height + bottom, 0.5);
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
