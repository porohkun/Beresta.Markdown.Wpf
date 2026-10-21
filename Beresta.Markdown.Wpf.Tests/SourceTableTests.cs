namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Editor;
using Extensions;
using ICSharpCode.AvalonEdit.Rendering;
using Kit;

/// <summary>Таблица исходником стоит столбцами: перед разделителями — место до края столбца, а не пробелы.</summary>
public sealed class SourceTableTests
{
    private const string Table =
        "| Mode | What is edited | Count |\n|---|:---:|---:|\n| View | nothing | 0 |\n| **Controls** | tasks and `collapsibles` | 12 |\n| Blocks | the blocks under the caret | 345 |";

    [Fact]
    public void Разделители_всех_строк_на_одной_вертикали()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show(Table);

            for (var separator = 0; separator < 3; separator++)
            {
                Enumerable.Range(1, 5).Select(line => GlyphX(editor, line, separator)).Should().AllSatisfy(x => x.Should().BeApproximately(GlyphX(editor, 1, separator), 0.5));
            }

            window.Close();
        });
    }

    [Fact]
    public void Столбцы_по_центру_и_по_правому_краю()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show(Table);

            var centers = new[] { 1, 3, 4, 5 }.Select(line => (TextStart(editor, line, 1) + TextEnd(editor, line, 1)) / 2).ToList();
            centers.Should().AllSatisfy(x => x.Should().BeApproximately(centers[0], 1));
            var ends = new[] { 1, 3, 4, 5 }.Select(line => TextEnd(editor, line, 2)).ToList();
            ends.Should().AllSatisfy(x => x.Should().BeApproximately(ends[0], 0.5));
            window.Close();
        });
    }

    [Fact]
    public void Место_не_текст()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show(Table);
            var document = editor.TextEditor.Document;
            var line = document.GetLineByNumber(3);
            var separator = editor.SeparatorsOf(line)[0];
            editor.SourceCaretOffset = separator.Offset;

            EditingCommands.MoveRightByCharacter.Execute(null, editor.TextEditor.TextArea);
            editor.SourceCaretOffset.Should().Be(separator.Offset + 1);

            var view = editor.TextEditor.TextArea.TextView;
            var visual = view.GetOrConstructVisualLine(line);
            var start = visual.GetTextLineVisualXPosition(visual.TextLines[0], visual.GetVisualColumn(separator.Offset - line.Offset));
            var point = new Point(start + (separator.Before / 2), visual.VisualTop - view.VerticalOffset + 2);
            view.GetPosition(point)!.Value.Location.Column.Should().Be(separator.Offset - line.Offset + 1, "щелчок в месте — конец ячейки");

            editor.SelectSource(line.Offset, line.Length);
            editor.TextEditor.SelectedText.Should().Be("| View | nothing | 0 |");
            window.Close();
        });
    }

    [Fact]
    public void Правка_ячейки_сдвигает_весь_столбец()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show(Table);
            var before = GlyphX(editor, 1, 0);
            var line = editor.TextEditor.Document.GetLineByNumber(3);
            editor.SourceCaretOffset = editor.SeparatorsOf(line)[0].Offset - 1;

            editor.TextEditor.TextArea.PerformTextInput(" and something long");
            WpfHost.Settle(window);

            GlyphX(editor, 1, 0).Should().BeGreaterThan(before + 20);
            GlyphX(editor, 3, 0).Should().BeApproximately(GlyphX(editor, 1, 0), 0.5);
            window.Close();
        });
    }

    [Fact]
    public void Таблица_шире_окна_не_выравнивается()
    {
        WpfHost.Invoke(() =>
        {
            var wide = "| a | b |\n|---|---|\n| " + string.Concat(Enumerable.Repeat("long words ", 40)) + "| c |\n| d | e |";
            var (editor, window) = Show(wide, width: 300);

            editor.SeparatorsOf(editor.TextEditor.Document.GetLineByNumber(1)).Should().BeEmpty();
            window.Close();
        });
    }

    [Fact]
    public void Таблица_CSV_стоит_столбцами()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show("```csv\nCity,Population,\"Area, km2\"\nMoscow,13 010 112,2561\nNovosibirsk,1 633 595,502\n```", extension: new CsvTables());

            for (var separator = 0; separator < 2; separator++)
            {
                new[] { 2, 3, 4 }.Select(line => GlyphX(editor, line, separator)).Should().AllSatisfy(x => x.Should().BeApproximately(GlyphX(editor, 2, separator), 0.5));
            }

            window.Close();
        });
    }

    private static (MarkdownEditor Editor, Window Window) Show(string markdown, double width = double.NaN, MdExtension? extension = null)
    {
        var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Full, Width = width };
        if (extension is not null)
        {
            editor.Extensions.Add(extension);
        }

        var window = WpfHost.Show(new ScrollViewer { Content = editor }, 600);
        return (editor, window);
    }

    /// <summary>Левый край знака разделителя с номером в строке документа с единицы.</summary>
    private static double GlyphX(MarkdownEditor editor, int number, int separator)
    {
        var line = editor.TextEditor.Document.GetLineByNumber(number);
        var (offset, before, _) = editor.SeparatorsOf(line)[separator];
        return X(editor, line, offset) + before;
    }

    /// <summary>Начало текста ячейки с номером: после разделителя перед ней и пробелов.</summary>
    private static double TextStart(MarkdownEditor editor, int number, int cell)
    {
        var document = editor.TextEditor.Document;
        var line = document.GetLineByNumber(number);
        var at = editor.SeparatorsOf(line)[cell - 1].Offset + 1;
        while (document.GetCharAt(at) == ' ')
        {
            at++;
        }

        return X(editor, line, at);
    }

    /// <summary>Конец текста ячейки с номером: перед пробелами у разделителя после неё.</summary>
    private static double TextEnd(MarkdownEditor editor, int number, int cell)
    {
        var document = editor.TextEditor.Document;
        var line = document.GetLineByNumber(number);
        var at = editor.SeparatorsOf(line)[cell].Offset;
        while (document.GetCharAt(at - 1) == ' ')
        {
            at--;
        }

        return X(editor, line, at);
    }

    private static double X(MarkdownEditor editor, ICSharpCode.AvalonEdit.Document.DocumentLine line, int offset)
    {
        VisualLine visual = editor.TextEditor.TextArea.TextView.GetOrConstructVisualLine(line);
        return visual.GetTextLineVisualXPosition(visual.TextLines[0], visual.GetVisualColumn(offset - line.Offset));
    }
}
