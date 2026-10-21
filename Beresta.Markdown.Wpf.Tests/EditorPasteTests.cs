namespace Beresta.Markdown.Wpf.Tests;

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Editor;

/// <summary>
/// Вставка с форматированием: HTML из данных буфера обмена — Markdown на месте выделения одним шагом
/// отмены, без HTML — текст. Буфер обмена тесты не трогают: данные идут во внутренний вход вставки.
/// </summary>
public sealed class EditorPasteTests
{
    private const string Rich = "<p><b>bold</b> and <a href=\"https://x.org\">link</a></p><ul><li>one</li><li>two</li></ul>"
        + "<table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td><i>2</i></td></tr></table>";

    private const string RichMarkdown = "**bold** and [link](https://x.org)\n\n- one\n- two\n\n| A | B |\n| --- | --- |\n| 1 | *2* |";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HTML_становится_Markdown_на_месте_выделения(bool markers)
        => Check(Data(Format(Rich, markers), "plain"), $"Before {RichMarkdown} after");

    [Fact]
    public void Незнакомые_теги_вставляются_своим_текстом()
        => Check(Data(Format("<custom>unknown <b>x</b></custom>", markers: true), null), "Before unknown **x** after");

    [Fact]
    public void Без_HTML_вставляется_текст()
        => Check(Data(null, "plain *text*"), "Before plain *text* after");

    [Fact]
    public void Вставка_отменяется_одним_шагом()
    {
        WpfHost.Invoke(() =>
        {
            var editor = Editor();

            editor.PasteFormatted(Data(Format(Rich, markers: true), null));
            editor.Undo();

            editor.TextEditor.Text.Should().Be("Before PASTE after");
        });
    }

    [Fact]
    public void Ctrl_Shift_V_и_меню_исходника()
    {
        WpfHost.Invoke(() =>
        {
            var editor = Editor();
            editor.Resources.MergedDictionaries.Add(new() { Source = new("pack://application:,,,/Beresta.Markdown.Wpf.Editor;component/Themes/Generic.xaml") });
            var window = WpfHost.Show(new ScrollViewer { Content = editor });

            MarkdownEditor.PasteFormattedCommand.InputGestures.OfType<KeyGesture>()
                .Should().ContainSingle(gesture => gesture.Key == Key.V && gesture.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift));
            editor.TextEditor.TextArea.ContextMenu!.Items.OfType<MenuItem>().Select(item => item.Command)
                .Should().Equal(ApplicationCommands.Cut, ApplicationCommands.Copy, ApplicationCommands.Paste, MarkdownEditor.PasteFormattedCommand, ApplicationCommands.SelectAll);
            window.Close();
        });
    }

    /// <summary>Вставка данных на место выделенного <c>PASTE</c> и текст после неё.</summary>
    private static void Check(IDataObject data, string expected)
    {
        WpfHost.Invoke(() =>
        {
            var editor = Editor();

            editor.PasteFormatted(data);

            editor.TextEditor.Text.Replace("\r\n", "\n").Should().Be(expected);
        });
    }

    private static MarkdownEditor Editor()
    {
        var editor = new MarkdownEditor { Markdown = "Before PASTE after", EditMode = MdEditMode.Full };
        editor.TextEditor.Select(7, 5);
        return editor;
    }

    private static DataObject Data(string? html, string? text)
    {
        var data = new DataObject();
        if (html is not null)
        {
            data.SetData(DataFormats.Html, html);
        }

        if (text is not null)
        {
            data.SetData(DataFormats.UnicodeText, text);
        }

        return data;
    }

    /// <summary>
    /// Данные формата <c>HTML Format</c>: заголовок со смещениями в байтах и страница с фрагментом — с
    /// метками <c>StartFragment</c> и <c>EndFragment</c> или без них.
    /// </summary>
    private static string Format(string fragment, bool markers)
    {
        const string header = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
        var before = "<html><body>" + (markers ? "<!--StartFragment-->" : string.Empty);
        var after = (markers ? "<!--EndFragment-->" : string.Empty) + "</body></html>";
        var length = Encoding.UTF8.GetByteCount(string.Format(header, 0, 0, 0, 0));
        var start = length + Encoding.UTF8.GetByteCount(before);
        var end = start + Encoding.UTF8.GetByteCount(fragment);
        var total = end + Encoding.UTF8.GetByteCount(after);
        return string.Format(header, length, total, start, end) + before + fragment + after;
    }
}
