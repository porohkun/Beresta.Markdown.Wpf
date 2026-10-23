namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Editor;
using Presenters;
using ViewModels;

/// <summary>Правимые блоки — единицы: пункт списка и блок внутри плашки, а список и плашка вокруг нарисованы.</summary>
public sealed class EditorUnitTests
{
    /// <summary>Абзац, список из десяти пунктов, оповещение с двумя абзацами и абзац в конце.</summary>
    private static readonly string Document = string.Join(
        "\n",
        [
            "Intro paragraph",
            string.Empty,
            .. Enumerable.Range(0, 10).Select(i => $"- item {i}"),
            string.Empty,
            "> [!NOTE]",
            "> first inside",
            ">",
            "> second inside",
            string.Empty,
            "Tail paragraph"
        ]);

    [Fact]
    public void Курсор_в_пункте_окно_на_этом_пункте_а_список_нарисован()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();

            Open(editor, window, Line(5));

            editor.WindowLines.Should().Be((6, 6));
            Rendered(editor).Should().Contain(["item 2", "item 4"]).And.NotContain("item 3");
            Shown(editor).Select(Kind).Should().StartWith(["Paragraph", "BlankLine", "List", "SourceWindow", "List"]);
            window.Close();
        });
    }

    [Fact]
    public void Стрелки_водят_окно_по_пунктам()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(5));

            EditingCommands.MoveDownByLine.Execute(null, editor.TextEditor.TextArea);
            WpfHost.Settle(window);

            editor.WindowLines.Should().Be((7, 7));
            Rendered(editor).Should().Contain(["item 3", "item 5"]).And.NotContain("item 4");
            window.Close();
        });
    }

    [Fact]
    public void Enter_в_конце_пункта_растит_окно_внутри_списка()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(5) + "- item 3".Length);

            editor.TextEditor.TextArea.PerformTextInput("\n");
            editor.TextEditor.TextArea.PerformTextInput("new");
            editor.CommitText();
            WpfHost.Settle(window);

            editor.WindowLines.Should().Be((6, 7));
            Rendered(editor).Should().Contain(["item 2", "item 4"]);
            window.Close();
        });
    }

    [Fact]
    public void Выход_из_списка_ставит_окно_наверх()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(11) + "- item 9".Length);

            editor.TextEditor.TextArea.PerformTextInput("\n");
            editor.TextEditor.TextArea.PerformTextInput("\n");
            editor.CommitText();
            WpfHost.Settle(window);

            var (first, last) = editor.WindowLines!.Value;
            first.Should().Be(3);
            last.Should().BeGreaterThanOrEqualTo(13);
            Rendered(editor).Should().NotContain(text => text.StartsWith("item", StringComparison.Ordinal));
            window.Close();
        });
    }

    [Fact]
    public void Выделение_через_два_пункта_ставит_окно_на_оба()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(5));

            editor.SelectSource(Line(5) + 2, Line(6) + 3 - (Line(5) + 2));
            WpfHost.Settle(window);

            editor.WindowLines.Should().Be((6, 7));
            Shown(editor).Select(Kind).Should().StartWith(["Paragraph", "BlankLine", "List", "SourceWindow", "List"]);
            window.Close();
        });
    }

    [Fact]
    public void Абзац_внутри_плашки_правится_внутри_неё()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();

            Open(editor, window, Line(14) + 2);

            editor.WindowLines.Should().Be((15, 15));
            var callout = Shown(editor).OfType<CalloutVm>().Should().ContainSingle().Subject;
            callout.Blocks.Select(Kind).Should().Equal("SourceWindow", "BlankLine", "Paragraph");
            Rendered(editor).Should().Contain("second inside");
            window.Close();
        });
    }

    [Fact]
    public void Выделение_из_плашки_наружу_ставит_окно_наверх()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(14) + 2);

            editor.SelectSource(Line(14) + 2, Line(18) + 2 - (Line(14) + 2));
            WpfHost.Settle(window);

            var (first, last) = editor.WindowLines!.Value;
            first.Should().BeLessThanOrEqualTo(14);
            last.Should().Be(19);
            Shown(editor).OfType<CalloutVm>().Should().BeEmpty();
            window.Close();
        });
    }

    [Fact]
    public void Отмена_возвращает_окно_на_пункт_отменённой_правки()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show();
            Open(editor, window, Line(5) + "- item 3".Length);
            editor.TextEditor.TextArea.PerformTextInput(" edited");
            editor.CommitText();
            WpfHost.Settle(window);

            editor.SourceCaretOffset = Line(18) + " edited".Length;
            WpfHost.Settle(window);
            editor.WindowLines.Should().Be((19, 19));

            editor.Undo();
            WpfHost.Settle(window);

            editor.WindowLines.Should().Be((6, 6));
            Rendered(editor).Should().Contain("item 4");
            window.Close();
        });
    }

    private static (MarkdownEditor Editor, Window Window) Show()
    {
        var editor = new MarkdownEditor { Markdown = Document, EditMode = MdEditMode.Blocks };
        var window = WpfHost.Show(new ScrollViewer { Content = editor });
        return (editor, window);
    }

    /// <summary>Начало строки исходника с нуля.</summary>
    private static int Line(int line) => Document
        .Split('\n')
        .Take(line)
        .Sum(text => text.Length + 1);

    /// <summary>Курсор на место: в блочном режиме окно встаёт на его единицу.</summary>
    private static void Open(MarkdownEditor editor, Window window, int offset)
    {
        editor.SourceCaretOffset = offset;
        WpfHost.Settle(window);
    }

    private static List<BlockVm> Shown(MarkdownEditor editor) => editor.Items.Cast<BlockVm>().ToList();

    private static string Kind(BlockVm block) => block.GetType().Name[..^2];

    private static List<string> Rendered(MarkdownEditor editor)
        => Visuals.Descendants<MdTextBlock>(editor)
            .Select(text => text.TryGetMapRange(out var range) ? editor.TextMap.GetText(range) : string.Empty)
            .ToList();
}
