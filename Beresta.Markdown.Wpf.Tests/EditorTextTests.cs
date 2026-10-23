namespace Beresta.Markdown.Wpf.Tests;

using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Editor;
using Presenters;

/// <summary>Текст редактора живёт в AvalonEdit и уходит в Markdown; «только контролы» правят его с отменой.</summary>
public sealed class EditorTextTests
{
    [Fact]
    public void Внешний_текст_ложится_в_документ_и_сбрасывает_отмену()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one" };
            editor.SourceCaretOffset = 3;
            editor.InsertText(" two");
            editor.CanUndo.Should().BeTrue();

            editor.Markdown = "other";

            editor.TextEditor.Text.Should().Be("other");
            editor.CanUndo.Should().BeFalse();
            editor.SourceCaretOffset.Should().Be(0);
        });
    }

    [Fact]
    public void Свой_текст_уходит_в_Markdown_через_паузу_а_CommitText_отдаёт_его_сразу()
    {
        WpfHost.Invoke(() =>
        {
            var editor = Fast(new MarkdownEditor { Markdown = "one" });
            editor.SourceCaretOffset = 3;

            editor.InsertText(" two");
            editor.Markdown.Should().Be("one");
            Pump(150);
            editor.Markdown.Should().Be("one two");

            editor.InsertText(" three");
            editor.CommitText();
            editor.Markdown.Should().Be("one two three");
        });
    }

    [Fact]
    public void Набор_без_перерыва_уходит_не_реже_предела()
    {
        WpfHost.Invoke(() =>
        {
            var editor = Fast(new MarkdownEditor { Markdown = string.Empty });
            editor.CommitDelay = TimeSpan.FromSeconds(10);
            var watch = Stopwatch.StartNew();

            while (editor.Markdown == string.Empty && watch.ElapsedMilliseconds < 1000)
            {
                editor.InsertText("a");
                Pump(20);
            }

            editor.Markdown.Should().NotBeEmpty();
            watch.ElapsedMilliseconds.Should().BeLessThan(500);
        });
    }

    [Fact]
    public void Свой_текст_вернувшийся_через_привязку_не_сбрасывает_отмену()
    {
        WpfHost.Invoke(() =>
        {
            var source = new TextHolder { Text = "one" };
            var editor = new MarkdownEditor();
            editor.SetBinding(MarkdownViewer.MarkdownProperty, new Binding(nameof(TextHolder.Text)) { Source = source, Mode = BindingMode.TwoWay });
            editor.SourceCaretOffset = 3;

            editor.InsertText(" two");
            editor.CommitText();

            source.Text.Should().Be("one two");
            editor.CanUndo.Should().BeTrue();
            editor.TextEditor.Text.Should().Be("one two");
        });
    }

    [Fact]
    public void Отмена_ставит_курсор_на_место_правки_и_отдаёт_текст()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one two" };
            editor.SourceCaretOffset = 3;
            editor.InsertText(" and a half");
            editor.SourceCaretOffset = 0;

            editor.Undo();

            editor.TextEditor.Text.Should().Be("one two");
            editor.Markdown.Should().Be("one two");
            editor.SourceCaretOffset.Should().Be(3);
            editor.CanRedo.Should().BeTrue();

            editor.SourceCaretOffset = 0;
            editor.Redo();

            editor.Markdown.Should().Be("one and a half two");
            editor.SourceCaretOffset.Should().Be(14);
        });
    }

    [Fact]
    public void SourceChanged_приходит_на_каждую_правку()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one" };
            var changes = new List<MdSourceEdit>();
            editor.SourceChanged += (_, e) => changes.Add(e.Change);
            editor.SourceCaretOffset = 3;

            editor.InsertText("!");
            editor.SelectSource(0, 3);
            editor.InsertText("two");
            editor.Undo();

            changes.Should().Equal(new MdSourceEdit(3, 0, "!"), new MdSourceEdit(0, 3, "two"), new MdSourceEdit(0, 3, "one"));
        });
    }

    [Fact]
    public void Режим_выбирает_правят_ли_контролы_исходник()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor();

            editor.AreControlsEditable.Should().BeFalse();
            editor.EditMode = MdEditMode.Controls;
            editor.AreControlsEditable.Should().BeTrue();
            editor.EditMode = MdEditMode.View;
            editor.AreControlsEditable.Should().BeFalse();
        });
    }

    [Fact]
    public void Щелчок_по_флажку_в_только_контролах_отменяется_CtrlZ()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "- [ ] one", EditMode = MdEditMode.Controls };
            var window = WpfHost.Show(editor);

            Toggle(editor, window);
            editor.Markdown.Should().Be("- [x] one");
            editor.TextEditor.Text.Should().Be("- [x] one");

            ApplicationCommands.Undo.CanExecute(null, editor).Should().BeTrue();
            ApplicationCommands.Undo.Execute(null, editor);
            WpfHost.Settle(window);

            editor.Markdown.Should().Be("- [ ] one");
            Visuals.Descendants<MdTaskMark>(editor).Single().IsChecked.Should().BeFalse();
            window.Close();
        });
    }

    [Fact]
    public void Правку_контрола_приложение_может_взять_себе()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "- [ ] one", EditMode = MdEditMode.Controls };
            var window = WpfHost.Show(editor);
            editor.SourceEditRequested += (_, e) => e.Handled = true;

            Toggle(editor, window);

            editor.Markdown.Should().Be("- [ ] one");
            editor.CanUndo.Should().BeFalse();
            window.Close();
        });
    }

    [Fact]
    public void Правка_контрола_ложится_на_нынешний_текст_а_не_на_показанный()
    {
        WpfHost.Invoke(() =>
        {
            var editor = Fast(new MarkdownEditor { Markdown = "- [ ] one", EditMode = MdEditMode.Controls });
            var window = WpfHost.Show(editor);
            editor.SourceCaretOffset = 0;
            editor.InsertText("Tasks:\n\n");

            Toggle(editor, window);

            editor.Markdown.Should().Be("Tasks:\n\n- [x] one");
            window.Close();
        });
    }

    private static MarkdownEditor Fast(MarkdownEditor editor)
    {
        editor.CommitDelay = TimeSpan.FromMilliseconds(50);
        editor.CommitLimit = TimeSpan.FromMilliseconds(200);
        return editor;
    }

    private static void Toggle(MarkdownEditor editor, Window window)
    {
        var mark = Visuals.Descendants<MdTaskMark>(editor).Single();
        ((IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(mark).GetPattern(PatternInterface.Toggle)).Toggle();
        WpfHost.Settle(window);
    }

    /// <summary>Прокачивает очередь диспетчера заданное время: таймеры редактора успевают сработать.</summary>
    private static void Pump(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < milliseconds)
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () => frame.Continue = false);
            Dispatcher.PushFrame(frame);
            Thread.Sleep(5);
        }
    }

    /// <summary>Источник привязки с уведомлением о смене текста.</summary>
    private sealed class TextHolder : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public string Text
        {
            get;
            set
            {
                field = value;
                PropertyChanged?.Invoke(this, new(nameof(Text)));
            }
        } = string.Empty;
    }
}
