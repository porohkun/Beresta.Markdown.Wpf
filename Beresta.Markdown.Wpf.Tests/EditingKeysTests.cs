namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Documents;
using Editor;

/// <summary>
/// Клавиши правки Markdown: текст до и после, <c>|</c> — курсор. Каждое правило отменяется одним
/// шагом.
/// </summary>
public sealed class EditingKeysTests
{
    [Theory]
    [InlineData("- one|", "- one\n- |")]
    [InlineData("* one|", "* one\n* |")]
    [InlineData("+ one|", "+ one\n+ |")]
    [InlineData("1. one|", "1. one\n2. |")]
    [InlineData("9) one|", "9) one\n10) |")]
    [InlineData("- [x] done|", "- [x] done\n- [ ] |")]
    [InlineData("  - nested|", "  - nested\n  - |")]
    [InlineData("- one|two", "- one\n- |two")]
    public void Enter_в_пункте_начинает_новый_пункт(string before, string after)
        => Check(before, after, editor => editor.TextEditor.TextArea.PerformTextInput("\n"));

    [Theory]
    [InlineData("- one\n- |", "- one\n|")]
    [InlineData("1. one\n2. |", "1. one\n|")]
    [InlineData("- [ ] |", "|")]
    public void Enter_в_пустом_пункте_выходит_из_списка(string before, string after)
        => Check(before, after, editor => editor.TextEditor.TextArea.PerformTextInput("\n"));

    [Theory]
    [InlineData("text|", "text\n|")]
    [InlineData("```\n- code|\n```", "```\n- code\n|\n```")]
    public void Enter_вне_пункта_и_в_коде_только_переводит_строку(string before, string after)
        => Check(before, after, editor => editor.TextEditor.TextArea.PerformTextInput("\n"));

    [Theory]
    [InlineData("- a\n- b|", "- a\n  - b|")]
    [InlineData("1. a\n2. b|", "1. a\n   2. b|")]
    [InlineData("- [ ] a\n- [ ] b|", "- [ ] a\n  - [ ] b|")]
    [InlineData("- a\n- b|\n  more\n- c", "- a\n  - b|\n    more\n- c")]
    [InlineData("- a|", "- a|")]
    public void Tab_ставит_пункт_под_содержимое_предыдущего(string before, string after)
        => Check(before, after, editor => EditingCommands.TabForward.Execute(null, editor.TextEditor.TextArea));

    [Theory]
    [InlineData("- a\n  - b|", "- a\n- b|")]
    [InlineData("- a\n  - b|\n    more", "- a\n- b|\n  more")]
    public void Shift_Tab_возвращает_пункт_к_родителю(string before, string after)
        => Check(before, after, editor => EditingCommands.TabBackward.Execute(null, editor.TextEditor.TextArea));

    [Fact]
    public void Tab_вне_пункта_работает_как_в_AvalonEdit()
        => Check("text|", "text\t|", editor => EditingCommands.TabForward.Execute(null, editor.TextEditor.TextArea));

    [Theory]
    [InlineData("``|", "```|\n```")]
    [InlineData("``|\n\n```cs\ncode\n```", "```|\n```\n\n```cs\ncode\n```")]
    [InlineData("~~|", "~~~|\n~~~")]
    public void Набранное_ограждение_закрывается_строкой_ниже(string before, string after)
        => Check(before, after, editor => editor.TextEditor.TextArea.PerformTextInput(before.Contains('~') ? "~" : "`"));

    [Theory]
    [InlineData("``|\ncode\n```", "```|\ncode\n```")]
    [InlineData("```\n``|\n```", "```\n```|\n```")]
    [InlineData("text ``|", "text ```|")]
    public void Ограждение_с_парой_ниже_и_не_в_начале_строки_не_закрывается(string before, string after)
        => Check(before, after, editor => editor.TextEditor.TextArea.PerformTextInput("`"));

    [Theory]
    [InlineData("- a\n- b|", "\n")]
    [InlineData("- a\n- |", "\n")]
    [InlineData("- a\n- b|", "tab")]
    [InlineData("- a\n  - b|", "untab")]
    public void Правило_списка_отменяется_одним_шагом(string before, string action)
    {
        WpfHost.Invoke(() =>
        {
            var editor = Editor(before);
            var area = editor.TextEditor.TextArea;
            switch (action)
            {
                case "tab":
                    EditingCommands.TabForward.Execute(null, area);
                    break;
                case "untab":
                    EditingCommands.TabBackward.Execute(null, area);
                    break;
                default:
                    area.PerformTextInput(action);
                    break;
            }

            editor.Undo();

            editor.TextEditor.Text.Should().Be(before.Replace("|", string.Empty));
        });
    }

    [Fact]
    public void Закрывающее_ограждение_отменяется_одним_шагом()
    {
        WpfHost.Invoke(() =>
        {
            var editor = Editor("text\n\n``|");
            editor.TextEditor.TextArea.PerformTextInput("`");

            editor.Undo();

            editor.TextEditor.Text.Should().NotContain("```\n```");
            editor.TextEditor.Text.Should().StartWith("text\n\n``");
        });
    }

    [Fact]
    public void Вставка_берёт_только_текст()
    {
        WpfHost.Invoke(() =>
        {
            var area = new MarkdownEditor().TextEditor.TextArea;
            var both = new DataObject();
            both.SetData(DataFormats.Html, "<b>bold</b>");
            both.SetData(DataFormats.UnicodeText, "bold");
            var withText = new DataObjectPastingEventArgs(both, false, DataFormats.Html);
            var withoutText = new DataObjectPastingEventArgs(new DataObject(DataFormats.Html, "<b>bold</b>"), false, DataFormats.Html);

            area.RaiseEvent(withText);
            area.RaiseEvent(withoutText);

            withText.FormatToApply.Should().Be(DataFormats.UnicodeText);
            withoutText.CommandCancelled.Should().BeTrue();
        });
    }

    /// <summary>
    /// Текст с курсором на месте <c>|</c> — в редактор, действие, текст с курсором после. Перевод строки
    /// в однострочном тексте AvalonEdit берёт системный, поэтому <c>\r\n</c> сравнивается как <c>\n</c>.
    /// </summary>
    private static void Check(string before, string after, Action<MarkdownEditor> action)
    {
        WpfHost.Invoke(() =>
        {
            var editor = Editor(before);

            action(editor);

            editor.TextEditor.Text.Insert(editor.SourceCaretOffset, "|").Replace("\r\n", "\n").Should().Be(after);
        });
    }

    private static MarkdownEditor Editor(string marked)
    {
        var caret = marked.IndexOf('|');
        var editor = new MarkdownEditor { Markdown = marked.Remove(caret, 1) };
        editor.SourceCaretOffset = caret;
        return editor;
    }
}
