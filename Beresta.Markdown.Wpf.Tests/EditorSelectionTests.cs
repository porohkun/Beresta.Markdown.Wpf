namespace Beresta.Markdown.Wpf.Tests;

using System.ComponentModel;
using System.Windows.Data;
using Editor;

/// <summary>Выделение исходника наружу: свойства зависимости в обе стороны и событие на каждый сдвиг.</summary>
public sealed class EditorSelectionTests
{
    [Fact]
    public void Выделение_уходит_в_свойства_и_событие()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one two" };
            var moves = 0;
            editor.SourceSelectionChanged += (_, _) => moves++;

            editor.SelectSource(4, 3);

            (editor.SourceSelectionStart, editor.SourceSelectionLength).Should().Be((4, 3));
            moves.Should().Be(1);

            editor.SelectSource(1, 0);

            (editor.SourceSelectionStart, editor.SourceSelectionLength).Should().Be((1, 0));
            moves.Should().Be(2);
        });
    }

    [Fact]
    public void Набор_сдвигает_курсор_в_свойствах()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one" };
            editor.SourceCaretOffset = 3;
            var moves = 0;
            editor.SourceSelectionChanged += (_, _) => moves++;

            editor.InsertText(" two");

            editor.SourceSelectionStart.Should().Be(7);
            moves.Should().Be(1);
        });
    }

    [Fact]
    public void Свойства_выделения_ставят_выделение_в_исходнике()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one two" };

            editor.SourceSelectionStart = 4;
            editor.SourceSelectionLength = 3;

            editor.TextEditor.SelectedText.Should().Be("two");
        });
    }

    [Fact]
    public void Выделение_за_краем_прижимается_к_исходнику()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "one two" };

            editor.SourceSelectionStart = 5;
            editor.SourceSelectionLength = 100;

            (editor.SourceSelectionStart, editor.SourceSelectionLength).Should().Be((5, 2));
            editor.TextEditor.SelectedText.Should().Be("wo");
        });
    }

    [Fact]
    public void Выделение_привязывается_к_модели_в_обе_стороны()
    {
        WpfHost.Invoke(() =>
        {
            var model = new SelectionHolder();
            var editor = new MarkdownEditor { Markdown = "one two three" };
            editor.SetBinding(MarkdownEditor.SourceSelectionStartProperty, new Binding(nameof(SelectionHolder.Start)) { Source = model });
            editor.SetBinding(MarkdownEditor.SourceSelectionLengthProperty, new Binding(nameof(SelectionHolder.Length)) { Source = model });

            editor.SelectSource(4, 3);

            (model.Start, model.Length).Should().Be((4, 3));

            model.Start = 8;
            model.Length = 5;

            editor.TextEditor.SelectedText.Should().Be("three");
        });
    }

    /// <summary>Вьюмодель тулбара, которая держит выделение исходника.</summary>
    private sealed class SelectionHolder : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Start
        {
            get;
            set
            {
                field = value;
                PropertyChanged?.Invoke(this, new(nameof(Start)));
            }
        }

        public int Length
        {
            get;
            set
            {
                field = value;
                PropertyChanged?.Invoke(this, new(nameof(Length)));
            }
        }
    }
}
