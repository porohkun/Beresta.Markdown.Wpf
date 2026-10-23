namespace Beresta.Markdown.Wpf.Tests;

using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Editor;
using Presenters;

/// <summary>
/// Текст редактора живёт в AvalonEdit и уходит в Markdown после каждой правки, а отрисовка догоняет его по
/// таймеру; «только контролы» правят его с отменой.
/// </summary>
[Collection(Timing.Name)]
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
    public void Слитый_текст_извне_ложится_одной_правкой_и_сохраняет_курсор_и_отмену()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one two three", MergeExternalText = true };
            editor.SourceCaretOffset = 9;
            editor.InsertText("X");
            var changes = new List<MdSourceEdit>();
            editor.SourceChanged += (_, e) => changes.Add(e.Change);

            editor.Markdown = "ONE two tXhree";

            editor.TextEditor.Text.Should().Be("ONE two tXhree");
            changes.Should().Equal(new MdSourceEdit(0, 3, "ONE"));
            editor.SourceCaretOffset.Should().Be(10);
            editor.Undo();
            editor.TextEditor.Text.Should().Be("one two tXhree");
            editor.Undo();
            editor.TextEditor.Text.Should().Be("one two three");
        });
    }

    [Fact]
    public void Слитый_текст_не_уходит_обратно_и_не_режет_суррогатную_пару()
    {
        WpfHost.Invoke(() =>
        {
            var editor = Fast(new MarkdownEditor { Markdown = "a😀b", MergeExternalText = true });
            var changes = new List<MdSourceEdit>();
            editor.SourceChanged += (_, e) => changes.Add(e.Change);

            editor.Markdown = "a😃b";
            Pump(150);

            changes.Should().Equal(new MdSourceEdit(1, 2, "😃"));
            editor.Markdown.Should().Be("a😃b");
            editor.CanUndo.Should().BeTrue();
        });
    }

    [Fact]
    public void Слитый_текст_не_закрывает_окно()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one\n\ntwo\n\nthree", EditMode = MdEditMode.Blocks, MergeExternalText = true };
            var window = WpfHost.Show(new ScrollViewer { Content = editor });
            editor.SourceCaretOffset = 1;
            WpfHost.Settle(window);
            editor.WindowLines.Should().Be((1, 1));

            editor.Markdown = "one\n\ntwo\n\nTHREE";
            WpfHost.Settle(window);

            editor.WindowLines.Should().Be((1, 1));
            editor.SourceCaretOffset.Should().Be(1);
            window.Close();
        });
    }

    [Fact]
    public void ReplaceSource_правит_кусок_одним_шагом_отмены_а_курсор_остаётся_на_своём_тексте()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one two three" };
            editor.SourceCaretOffset = 9;

            editor.ReplaceSource(0, 3, "ONE!");

            editor.TextEditor.Text.Should().Be("ONE! two three");
            editor.SourceCaretOffset.Should().Be(10);
            editor.CommitText();
            editor.Markdown.Should().Be("ONE! two three");
            editor.Undo();
            editor.TextEditor.Text.Should().Be("one two three");
        });
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(4, 0)]
    [InlineData(1, 3)]
    public void ReplaceSource_за_краем_исходника_бросает(int start, int length)
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one" };

            var replace = () => editor.ReplaceSource(start, length, "x");

            replace.Should().Throw<ArgumentOutOfRangeException>();
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Правка_извне_не_ведёт_страницу_к_курсору(bool merge)
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n\n", Enumerable.Range(0, 120).Select(i => $"Paragraph {i}"));
            var editor = new MarkdownEditor { Markdown = text, EditMode = MdEditMode.Full, MergeExternalText = true };
            var scroller = new ScrollViewer { Content = editor };
            var window = WpfHost.Show(scroller);
            var caret = text.IndexOf("Paragraph 100", StringComparison.Ordinal);
            editor.SourceCaretOffset = caret;
            WpfHost.Settle(window);
            scroller.ScrollToVerticalOffset(0);
            WpfHost.Settle(window);

            if (merge)
            {
                editor.Markdown = "Inserted " + text;
            }
            else
            {
                editor.ReplaceSource(0, 0, "Inserted ");
            }

            WpfHost.Settle(window);

            editor.SourceCaretOffset.Should().Be(caret + "Inserted ".Length);
            scroller.VerticalOffset.Should().BeApproximately(0, 40);
            window.Close();
        });
    }

    [Fact]
    public void Правка_сразу_уходит_в_Markdown_а_отрисовка_догоняет_через_паузу_и_по_CommitText()
    {
        WpfHost.Invoke(() =>
        {
            var editor = Fast(new MarkdownEditor { Markdown = "one" });
            editor.SourceCaretOffset = 3;

            editor.InsertText(" two");
            editor.Markdown.Should().Be("one two");
            editor.Source.Should().Be("one");
            Pump(150);
            editor.Source.Should().Be("one two");

            editor.InsertText(" three");
            editor.CommitText();
            editor.Source.Should().Be("one two three");
        });
    }

    [Fact]
    public void Набор_без_перерыва_рисуется_не_реже_предела()
    {
        WpfHost.Invoke(() =>
        {
            var editor = Fast(new MarkdownEditor { Markdown = string.Empty });
            editor.CommitDelay = TimeSpan.FromSeconds(10);
            var watch = Stopwatch.StartNew();

            while (editor.Source == string.Empty && watch.ElapsedMilliseconds < 1000)
            {
                editor.InsertText("a");
                Pump(20);
            }

            editor.Source.Should().NotBeEmpty();
            watch.ElapsedMilliseconds.Should().BeLessThan(500);
        });
    }

    [Fact]
    public void Правка_из_нескольких_шагов_уходит_в_Markdown_одним_текстом()
    {
        WpfHost.Invoke(() =>
        {
            var source = new TextHolder { Text = "1. a\n2. b" };
            var editor = new MarkdownEditor();
            editor.SetBinding(MarkdownViewer.MarkdownProperty, new Binding(nameof(TextHolder.Text)) { Source = source, Mode = BindingMode.TwoWay });
            editor.SourceCaretOffset = 4;
            source.Sets = 0;

            editor.TextEditor.TextArea.PerformTextInput("\n");

            source.Text.Replace("\r\n", "\n").Should().Be("1. a\n2. \n3. b");
            source.Sets.Should().Be(1);
        });
    }

    [Theory]
    [InlineData(MdEditMode.View)]
    [InlineData(MdEditMode.Controls)]
    [InlineData(MdEditMode.Full)]
    public void Потеря_фокуса_рисует_набранное_сразу(MdEditMode mode)
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one", EditMode = mode };
            editor.SourceCaretOffset = 3;
            editor.InsertText(" two");
            editor.Source.Should().Be("one");

            editor.LoseFocus();

            editor.Source.Should().Be("one two");
        });
    }

    [Fact]
    public void Потеря_фокуса_в_блочном_режиме_рисует_набранное_и_закрывает_окно()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one\n\ntwo", EditMode = MdEditMode.Blocks };
            var window = WpfHost.Show(new ScrollViewer { Content = editor });
            editor.SourceCaretOffset = 3;
            WpfHost.Settle(window);
            editor.WindowLines.Should().NotBeNull();
            editor.InsertText("!");

            editor.LoseFocus();
            editor.Source.Should().Be("one!\n\ntwo");
            WpfHost.Settle(window);

            editor.WindowLines.Should().BeNull();
            window.Close();
        });
    }

    [Fact]
    public void Своя_правка_сразу_уходит_через_привязку_и_не_сбрасывает_отмену()
    {
        WpfHost.Invoke(() =>
        {
            var source = new TextHolder { Text = "one" };
            var editor = new MarkdownEditor();
            editor.SetBinding(MarkdownViewer.MarkdownProperty, new Binding(nameof(TextHolder.Text)) { Source = source, Mode = BindingMode.TwoWay });
            editor.SourceCaretOffset = 3;

            editor.InsertText(" two");

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
                Sets++;
                PropertyChanged?.Invoke(this, new(nameof(Text)));
            }
        } = string.Empty;

        /// <summary>Сколько раз текст записали.</summary>
        public int Sets { get; set; }
    }
}
