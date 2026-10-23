namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Editor;
using ICSharpCode.AvalonEdit.Editing;
using Presenters;

/// <summary>
/// Окно правимых блоков на документе в 10 000 строк: после каждого шага в видимой части AvalonEdit
/// нет строк вне окна, а курсор виден.
/// </summary>
public sealed class EditorWindowTests
{
    /// <summary>Блок с номером: абзац в три строки, а каждый сотый с полусотого — блок кода.</summary>
    private static readonly string Document = string.Join(
        "\n\n",
        Enumerable.Range(0, 2500).Select(i => i % 100 == 50
            ? $"```\ncode {i}\n```"
            : $"Paragraph {i} one\nParagraph {i} two\nParagraph {i} three"));

    [Fact]
    public void Стрелки_через_границу_блоков_переводят_окно_и_прежний_блок_снова_нарисован()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10));
            editor.WindowLines.Should().Be((41, 43));
            Rendered(editor).Should().NotContain(text => text.StartsWith("Paragraph 10 ", StringComparison.Ordinal));

            for (var i = 0; i < 4; i++)
            {
                Execute(editor, window, EditingCommands.MoveDownByLine);
            }

            editor.WindowLines.Should().Be((45, 47));
            Rendered(editor).Should().Contain(text => text.StartsWith("Paragraph 10 one", StringComparison.Ordinal));
            window.Close();
        });
    }

    [Fact]
    public void Shift_со_стрелками_за_край_растит_окно()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10));

            for (var i = 0; i < 4; i++)
            {
                Execute(editor, window, EditingCommands.SelectDownByLine);
            }

            editor.WindowLines.Should().Be((41, 47));
            window.Close();
        });
    }

    [Fact]
    public void PageDown_уводит_курсор_примерно_на_экран()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10));
            var before = editor.TextEditor.TextArea.Caret.Line;

            Execute(editor, window, EditingCommands.MoveDownByPage);

            editor.TextEditor.TextArea.Caret.Line.Should().BeInRange(before + 8, before + 40);
            window.Close();
        });
    }

    [Fact]
    public void Щелчок_по_нарисованному_ставит_окно_на_блок_и_курсор_на_знак_под_мышью()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10));

            editor.PlaceCaretAt(At(editor, "Paragraph 12 two", "two")).Should().BeTrue();
            WpfHost.Settle(window);

            editor.WindowLines.Should().Be((49, 51));
            editor.SourceCaretOffset.Should().Be(Line(12) + "Paragraph 12 one\nParagraph 12 ".Length);
            ShouldBeSound(editor);
            window.Close();
        });
    }

    [Fact]
    public void Щелчок_после_правки_выше_ставит_курсор_на_тот_же_знак_пока_разбор_не_лёг()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10));
            editor.TextEditor.TextArea.PerformTextInput("New ");
            WpfHost.Settle(window);

            editor.PlaceCaretAt(At(editor, "Paragraph 12 two", "two")).Should().BeTrue();

            editor.SourceCaretOffset.Should().Be(Line(12) + "New ".Length + "Paragraph 12 one\nParagraph 12 ".Length);
            window.Close();
        });
    }

    [Fact]
    public void Щелчок_правее_конца_строки_ставит_курсор_в_её_конец()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10));
            var end = At(editor, "Paragraph 12 one", "one");

            editor.PlaceCaretAt(new(end.X + 300, end.Y)).Should().BeTrue();
            WpfHost.Settle(window);

            editor.SourceCaretOffset.Should().Be(Line(12) + "Paragraph 12 one".Length);
            window.Close();
        });
    }

    [Fact]
    public void Двойной_щелчок_по_нарисованному_выделяет_слово_в_исходнике()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10));
            editor.PlaceCaretAt(At(editor, "Paragraph 12 two", "two")).Should().BeTrue();
            WpfHost.Settle(window);
            var second = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent };
            typeof(MouseButtonEventArgs).GetProperty(nameof(MouseButtonEventArgs.ClickCount))!.SetValue(second, 2);

            editor.RaiseEvent(second);

            second.Handled.Should().BeTrue();
            editor.SourceSelectionStart.Should().Be(Line(12) + "Paragraph 12 one\nParagraph 12 ".Length);
            editor.SourceSelectionLength.Should().Be("two".Length);
            editor.SelectClickedWord().Should().BeFalse("слово выделяет только двойной щелчок сразу после щелчка по нарисованному");
            window.Close();
        });
    }

    [Fact]
    public void Набор_и_перевод_строки_растят_окно_а_не_сжимают()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10) + "Paragraph 10 one".Length);
            var area = editor.TextEditor.TextArea;

            area.PerformTextInput("\n\nA new paragraph\n\n");
            WpfHost.Settle(window);
            editor.WindowLines.Should().Be((41, 47));
            ShouldBeSound(editor);

            editor.CommitText();
            WpfHost.Settle(window);
            editor.WindowLines.Should().Be((41, 47));
            ShouldBeSound(editor);
            window.Close();
        });
    }

    [Fact]
    public void Backspace_в_начале_блока_сливает_блоки_и_окно_встаёт_на_оба()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(11));

            Execute(editor, window, EditingCommands.Backspace);
            editor.CommitText();
            WpfHost.Settle(window);

            editor.WindowLines.Should().Be((41, 46));
            ShouldBeSound(editor);
            window.Close();
        });
    }

    [Fact]
    public void Отмена_после_ухода_курсора_возвращает_курсор_и_окно()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10));
            editor.TextEditor.TextArea.PerformTextInput("Edited ");
            editor.CommitText();
            WpfHost.Settle(window);

            editor.SourceCaretOffset = editor.TextEditor.Document.GetLineByNumber(81).Offset;
            WpfHost.Settle(window);
            editor.WindowLines.Should().Be((81, 83));

            editor.Undo();
            WpfHost.Settle(window);

            editor.SourceCaretOffset.Should().Be(Line(10));
            editor.WindowLines.Should().Be((41, 43));
            ShouldBeSound(editor);
            window.Close();
        });
    }

    [Fact]
    public void Незакрытое_ограждение_растит_окно_до_следующего_а_отмена_сжимает()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10));

            editor.TextEditor.TextArea.PerformTextInput("```\n");
            editor.CommitText();
            WpfHost.Settle(window);
            editor.WindowLines.Should().Be((41, 202));
            ShouldBeSound(editor);

            editor.Undo();
            WpfHost.Settle(window);
            editor.WindowLines.Should().Be((41, 43));
            ShouldBeSound(editor);
            window.Close();
        });
    }

    [Fact]
    public void Ctrl_A_ставит_окно_на_все_строки_и_раскладывает_только_видимые()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10));
            var element = editor.WindowElement;

            Execute(editor, window, ApplicationCommands.SelectAll);

            editor.WindowLines.Should().Be((1, editor.TextEditor.Document.LineCount));
            editor.LaidOutLines.Should().BeInRange(1, 99);
            editor.WindowElement.Should().BeSameAs(element);
            element.IsDescendantOf(editor).Should().BeTrue();
            ShouldBeSound(editor);
            window.Close();
        });
    }

    [Fact]
    public void Полный_режим_ставит_окно_на_весь_текст()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show(MdEditMode.Full);

            editor.WindowLines.Should().Be((1, editor.TextEditor.Document.LineCount));
            editor.LaidOutLines.Should().BeInRange(1, 99);
            editor.ForeignLines().Should().BeEmpty();
            Rendered(editor).Should().BeEmpty();
            window.Close();
        });
    }

    [Fact]
    public void В_блочном_режиме_окна_нет_пока_курсор_не_поставили()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();

            editor.WindowLines.Should().BeNull();

            Open(editor, window, Line(3));
            editor.WindowLines.Should().Be((13, 15));

            editor.EditMode = MdEditMode.Controls;
            WpfHost.Settle(window);
            editor.WindowLines.Should().BeNull();
            window.Close();
        });
    }

    [Fact]
    public void Протяжка_до_края_экрана_растит_окно_на_блоки_под_мышью()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(10));
            var scroll = (ScrollViewer)editor.Parent;
            var below = editor.WindowElement.TranslatePoint(new(10, scroll.ViewportHeight), editor);

            editor.ExtendSelectionTo(below);
            WpfHost.Settle(window);

            var (first, last) = editor.WindowLines!.Value;
            first.Should().Be(41);
            last.Should().BeGreaterThan(47);
            editor.SourceSelectionLength.Should().BePositive();
            editor.ForeignLines().Should().BeEmpty();
            window.Close();
        });
    }

    private static (MarkdownEditor Editor, Window Window) Show(MdEditMode mode = MdEditMode.Blocks)
    {
        var editor = new MarkdownEditor { Markdown = Document, EditMode = mode };
        var window = WpfHost.Show(new ScrollViewer { Content = editor });
        return (editor, window);
    }

    /// <summary>Начало первой строки блока с номером.</summary>
    private static int Line(int block) => Document
        .Split('\n')
        .Take(4 * block)
        .Sum(line => line.Length + 1);

    /// <summary>Точка у левого края первого знака куска в нарисованном тексте, который начинается строкой <paramref name="line"/>.</summary>
    private static Point At(MarkdownEditor editor, string line, string piece)
    {
        var position = editor.TextMap.Text.IndexOf(line, StringComparison.Ordinal) + line.IndexOf(piece, StringComparison.Ordinal);
        var box = editor.GetRangeRects(new(position, 1))[0];
        return new(box.Left + (box.Width / 4), box.Top + (box.Height / 2));
    }

    /// <summary>Курсор на место: в блочном режиме окно встаёт на его блок.</summary>
    private static void Open(MarkdownEditor editor, Window window, int offset)
    {
        editor.SourceCaretOffset = offset;
        WpfHost.Settle(window);
        ShouldBeSound(editor);
    }

    private static void Execute(MarkdownEditor editor, Window window, RoutedCommand command)
    {
        command.Execute(null, editor.TextEditor.TextArea);
        WpfHost.Settle(window);
        ShouldBeSound(editor);
    }

    private static void ShouldBeSound(MarkdownEditor editor)
    {
        editor.ForeignLines().Should().BeEmpty();
        editor.IsCaretVisible().Should().BeTrue();
    }

    private static List<string> Rendered(MarkdownEditor editor)
        => Visuals.Descendants<MdTextBlock>(editor)
            .Select(text => Text(editor, text))
            .ToList();

    private static string Text(MarkdownEditor editor, MdTextBlock text)
        => text.TryGetMapRange(out var range) ? editor.TextMap.GetText(range) : string.Empty;
}
