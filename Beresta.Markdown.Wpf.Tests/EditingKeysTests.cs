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
    [InlineData("> - one|", "> - one\n> - |")]
    [InlineData("> > 1. one|", "> > 1. one\n> > 2. |")]
    [InlineData("> [!NOTE]\n> - [x] done|", "> [!NOTE]\n> - [x] done\n> - [ ] |")]
    [InlineData("> - one\n> - |", "> - one\n> |")]
    [InlineData("> 1. a|\n> 2. b", "> 1. a\n> 2. |\n> 3. b")]
    public void Enter_продолжает_список_в_цитате(string before, string after)
        => Check(before, after, editor => editor.TextEditor.TextArea.PerformTextInput("\n"));

    [Theory]
    [InlineData("> - a\n> - b|", false, "> - a\n>   - b|")]
    [InlineData("> - a\n>   - b|", true, "> - a\n> - b|")]
    [InlineData("> 1. a\n> 2. b|\n> 3. c", false, "> 1. a\n>    2. b|\n> 2. c")]
    public void Tab_сдвигает_пункт_в_цитате(string before, bool outdent, string after)
        => Check(before, after, editor => (outdent ? EditingCommands.TabBackward : EditingCommands.TabForward).Execute(null, editor.TextEditor.TextArea));

    [Theory]
    [InlineData("text|", "text\n|")]
    [InlineData("```\n- code|\n```", "```\n- code\n|\n```")]
    [InlineData("- item\n    - nested\n        ```\n        - code|\n        ```", "- item\n    - nested\n        ```\n        - code\n        |\n        ```")]
    public void Enter_вне_пункта_и_в_коде_только_переводит_строку(string before, string after)
        => Check(before, after, editor => editor.TextEditor.TextArea.PerformTextInput("\n"));

    [Theory]
    [InlineData("> text|", "> text\n> |")]
    [InlineData("> te|xt", "> te\n> |xt")]
    [InlineData("> text\n> |", "> text\n|")]
    [InlineData("> > text\n> > |", "> > text\n> |")]
    [InlineData("> ```\n> - code|\n> ```", "> ```\n> - code\n> |\n> ```")]
    [InlineData("> ```\n> |\n> ```", "> ```\n> \n> |\n> ```")]
    public void Enter_в_цитате_продолжает_её_знаки(string before, string after)
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

    [Theory]
    [InlineData("- a\n- b\n  - b1\n- c\n- d", 5, false, "- a\n  - b\n    - b1\n  - c\n- d")]
    [InlineData("1. a\n2. b\n3. c\n4. d", 4, false, "1. a\n   2. b\n   3. c\n2. d")]
    [InlineData("- a\n  - b\n    - b1\n  - c\n- d", 5, true, "- a\n- b\n  - b1\n- c\n- d")]
    public void Tab_на_выделении_сдвигает_каждый_пункт(string text, int until, bool outdent, string expected)
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = text, EditMode = MdEditMode.Full };
            var document = editor.TextEditor.Document;
            var start = document.GetLineByNumber(2).Offset;
            editor.TextEditor.Select(start, document.GetLineByNumber(until).Offset - start);

            (outdent ? EditingCommands.TabBackward : EditingCommands.TabForward).Execute(null, editor.TextEditor.TextArea);

            editor.TextEditor.Text.Replace("\r\n", "\n").Should().Be(expected);
            editor.TextEditor.Document.UndoStack.Undo();
            editor.TextEditor.Text.Replace("\r\n", "\n").Should().Be(text);
        });
    }

    [Fact]
    public void Tab_вне_пункта_работает_как_в_AvalonEdit()
        => Check("text|", "text\t|", editor => EditingCommands.TabForward.Execute(null, editor.TextEditor.TextArea));

    [Theory]
    [InlineData("```|\n```", "````|\n````")]
    [InlineData("````|\ncode\n````\n\ntext", "`````|\ncode\n`````\n\ntext")]
    [InlineData("~~~|\n~~~", "~~~~|\n~~~~")]
    [InlineData("```|\n````", "````|\n````")]
    [InlineData("```|\n```cs", "````|\n```cs")]
    public void Закрывающее_ограждение_растёт_вместе_с_открывающим(string before, string after)
        => Check(before, after, editor => editor.TextEditor.TextArea.PerformTextInput(before.Contains('~') ? "~" : "`"));

    [Theory]
    [InlineData("``|", "```|\n```")]
    [InlineData("``|\n\n```cs\ncode\n```", "```|\n```\n\n```cs\ncode\n```")]
    [InlineData("~~|", "~~~|\n~~~")]
    [InlineData("> ``|", "> ```|\n> ```")]
    [InlineData("- item\n    - nested\n        ``|", "- item\n    - nested\n        ```|\n        ```")]
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

    [Theory]
    [InlineData("a «word» b", "a **«word»** b")]
    [InlineData("a **«word»** b", "a «word» b")]
    [InlineData("a «**word**» b", "a «word» b")]
    [InlineData("a __«word»__ b", "a «word» b")]
    [InlineData("a «word »b", "a **«word»** b")]
    [InlineData("a ***«word»*** b", "a *«word»* b")]
    [InlineData("- «one\n- two\n\n> three»", "- **«one**\n- **two**\n\n> **three»**")]
    [InlineData("- **«one**\n- **two»**", "- «one\n- two»")]
    [InlineData("«# Title»", "# **«Title»**")]
    [InlineData("- [ ] «task»", "- [ ] **«task»**")]
    public void Ctrl_B_обрамляет_выделение_и_снимает_обрамление(string before, string after)
        => CheckSelection(before, after, editor => EditingCommands.ToggleBold.Execute(null, editor));

    [Theory]
    [InlineData("a «word» b", "a *«word»* b")]
    [InlineData("a *«word»* b", "a «word» b")]
    [InlineData("a _«word»_ b", "a «word» b")]
    [InlineData("a **«word»** b", "a ***«word»*** b")]
    [InlineData("a ***«word»*** b", "a **«word»** b")]
    public void Ctrl_I_обрамляет_выделение_курсивом(string before, string after)
        => CheckSelection(before, after, editor => EditingCommands.ToggleItalic.Execute(null, editor));

    [Theory]
    [InlineData("a wo|rd b", "a **wo|rd** b")]
    [InlineData("a **wo|rd** b", "a wo|rd b")]
    [InlineData("|word", "**|word**")]
    [InlineData("a | b", "a **|** b")]
    [InlineData("a **|** b", "a | b")]
    public void Ctrl_B_без_выделения_берёт_слово_или_ставит_пару(string before, string after)
        => Check(before, after, editor => EditingCommands.ToggleBold.Execute(null, editor), MdEditMode.Full);

    [Fact]
    public void Ctrl_B_в_просмотре_не_правит()
        => Check("a wo|rd b", "a wo|rd b", editor => EditingCommands.ToggleBold.Execute(null, editor));

    [Theory]
    [InlineData("1. a|\n2. b\n3. c", "1. a\n2. |\n3. b\n4. c")]
    [InlineData("1) a|\n2) b", "1) a\n2) |\n3) b")]
    [InlineData("9. a|\n10. b", "9. a\n10. |\n11. b")]
    [InlineData("1. a|\n   - x\n2. b", "1. a\n2. |\n   - x\n3. b")]
    [InlineData("1. a|\n\n- other\n2. b", "1. a\n2. |\n\n- other\n2. b")]
    [InlineData("1. a\n1. b|\n1. c", "1. a\n1. b\n1. |\n1. c")]
    public void Enter_перенумеровывает_пункты_ниже(string before, string after)
        => Check(before, after, editor => editor.TextEditor.TextArea.PerformTextInput("\n"));

    [Theory]
    [InlineData("1. a\n2. b|\n3. c", "1. a\n   2. b|\n2. c", false)]
    [InlineData("1. a\n   1. x|\n2. b", "1. a\n2. x|\n3. b", true)]
    public void Сдвиг_пункта_перенумеровывает_список(string before, string after, bool outdent)
        => Check(before, after, editor => (outdent ? EditingCommands.TabBackward : EditingCommands.TabForward).Execute(null, editor.TextEditor.TextArea));

    [Theory]
    [InlineData("1. a\n2. b\n|3. c\n4. d", "1. a\n2. b|3. c\n3. d", false)]
    [InlineData("1. a\n2. b|\n3. c\n4. d", "1. a\n2. b|3. c\n3. d", true)]
    [InlineData("1. a\n2. b\n3. c|", "1. a\n2. b\n3. |", false)]
    public void Удаление_строки_пункта_перенумеровывает_список(string before, string after, bool delete)
        => Check(before, after, editor => (delete ? EditingCommands.Delete : EditingCommands.Backspace).Execute(null, editor.TextEditor.TextArea));

    [Fact]
    public void Удалённые_строки_перенумеровываются_и_место_текста_остаётся()
    {
        WpfHost.Invoke(() =>
        {
            var editor = Editor("8. a\n|9. b\n10. c");
            editor.TextEditor.Select(5, 5);

            EditingCommands.Delete.Execute(null, editor.TextEditor.TextArea);

            editor.TextEditor.Text.Replace("\r\n", "\n").Should().Be("8. a\n9.  c");
        });
    }

    [Theory]
    [InlineData("1. a|\n2. b", "enter")]
    [InlineData("1. a\n2. b|\n3. c", "tab")]
    [InlineData("1. a\n2. b\n|3. c\n4. d", "backspace")]
    [InlineData("- one\n- t|wo\n\n> three", "bold")]
    public void Перенумерация_и_обрамление_отменяются_одним_шагом(string before, string action)
    {
        WpfHost.Invoke(() =>
        {
            var editor = Editor(before, MdEditMode.Full);
            var area = editor.TextEditor.TextArea;
            switch (action)
            {
                case "tab":
                    EditingCommands.TabForward.Execute(null, area);
                    break;
                case "backspace":
                    EditingCommands.Backspace.Execute(null, area);
                    break;
                case "bold":
                    editor.TextEditor.Select(2, editor.TextEditor.Document.TextLength - 2);
                    EditingCommands.ToggleBold.Execute(null, editor);
                    break;
                default:
                    area.PerformTextInput("\n");
                    break;
            }

            editor.TextEditor.Text.Should().NotBe(before.Replace("|", string.Empty));
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
    private static void Check(string before, string after, Action<MarkdownEditor> action, MdEditMode mode = MdEditMode.View)
    {
        WpfHost.Invoke(() =>
        {
            var editor = Editor(before, mode);

            action(editor);

            editor.TextEditor.Text.Insert(editor.SourceCaretOffset, "|").Replace("\r\n", "\n").Should().Be(after);
        });
    }

    /// <summary>
    /// Текст с выделением между <c>«</c> и <c>»</c> — в редактор в полном режиме, действие, текст с
    /// выделением после.
    /// </summary>
    private static void CheckSelection(string before, string after, Action<MarkdownEditor> action)
    {
        WpfHost.Invoke(() =>
        {
            var start = before.IndexOf('«');
            var end = before.IndexOf('»') - 1;
            var editor = new MarkdownEditor { Markdown = before.Replace("«", string.Empty).Replace("»", string.Empty), EditMode = MdEditMode.Full };
            editor.TextEditor.Select(start, end - start);

            action(editor);

            var text = editor.TextEditor;
            text.Text
                .Insert(text.SelectionStart + text.SelectionLength, "»")
                .Insert(text.SelectionStart, "«")
                .Replace("\r\n", "\n")
                .Should().Be(after);
        });
    }

    private static MarkdownEditor Editor(string marked, MdEditMode mode = MdEditMode.View)
    {
        var caret = marked.IndexOf('|');
        var editor = new MarkdownEditor { Markdown = marked.Remove(caret, 1), EditMode = mode };
        editor.SourceCaretOffset = caret;
        return editor;
    }
}
