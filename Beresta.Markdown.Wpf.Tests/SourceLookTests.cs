namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Editor;
using Extensions;
using ICSharpCode.AvalonEdit.Rendering;
using Markdig.Extensions.TaskLists;
using ViewModels;

/// <summary>
/// Оформление исходника: вьюер описывает куски по виду элементов, знаки разметки — место элемента без
/// мест детей, а редактор рисует их шрифтами и цветами из ключей темы.
/// </summary>
public sealed class SourceLookTests
{
    private const string Text =
        "## Head *it*\n\nA **b** ~~s~~ [c](http://u) `d`\n\n> q\n\n- [ ] t\n\n```cs\ncode\n```\n\n| x | y |\n|---|---|\n| 1 | 2 |";

    [Fact]
    public void Встроенные_элементы_описаны_своим_видом()
    {
        var look = Describe(Text);

        look.Pieces.Should().Contain([
            (0, 12, "Heading2"),
            (At("it"), 2, "Italic"),
            (At("b**"), 1, "Bold"),
            (At("s~~"), 1, "Strike"),
            (At("c]"), 1, "Link"),
            (At("d`"), 1, "InlineCode"),
            (At("code"), 4, "CodeBlock")]);
        look.Lines.Should().Contain([(4, 4, "Quote"), (8, 10, "CodeBlock")]);
    }

    [Fact]
    public void Знаки_разметки_место_элемента_без_мест_детей()
    {
        var markup = Describe(Text).Pieces
            .Where(piece => piece.Look == "Source.Markup")
            .SelectMany(piece => Text.Substring(piece.Start, piece.Length).Split('\n'))
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();

        markup.Should().Contain(["##", "*", "**", "~~", "[", "](http://u)", "`", ">", "-", "```cs", "```", "|", "|---|---|"]);
        markup.Should().NotContain(piece => piece.Contains("Head") || piece.Contains("code") || piece.Contains('x'));
    }

    [Fact]
    public void Описание_расширения_идёт_последним()
    {
        var tasks = new LambdaExtension
        {
            Inline = (inline, _) => inline is TaskList task ? new TaskVm(task.Checked) : null,
            Describe = (node, look) => look.Mark(node.Source.Start, node.Source.Length, "Probe")
        };

        var look = Describe(Text, tasks);

        look.Pieces[^1].Should().Be((At("[ ]"), 3, "Probe"));
    }

    [Fact]
    public void Редактор_рисует_куски_шрифтами_и_цветами_темы()
    {
        WpfHost.Invoke(() =>
        {
            var tasks = new LambdaExtension
            {
                Inline = (inline, _) => inline is TaskList task ? new TaskVm(task.Checked) : null,
                Describe = (node, look) => look.Mark(node.Source.Start, node.Source.Length, "Probe")
            };
            var editor = new MarkdownEditor { Markdown = Text, EditMode = MdEditMode.Full };
            editor.Extensions.Add(tasks);
            editor.Resources.MergedDictionaries.Add(new() { Source = new("pack://application:,,,/Beresta.Markdown.Wpf.Editor;component/Themes/Generic.xaml") });
            editor.Resources["Md.Probe.Foreground"] = Brushes.Purple;
            var window = WpfHost.Show(new ScrollViewer { Content = editor });

            Run(editor, "Head").FontRenderingEmSize.Should().Be((double)editor.FindResource("Md.Heading2.FontSize"));
            Run(editor, "Head").Typeface.Weight.Should().Be((FontWeight)editor.FindResource("Md.Heading2.FontWeight"));
            Run(editor, "it").Typeface.Style.Should().Be(FontStyles.Italic);
            Run(editor, "b**").Typeface.Weight.Should().Be(FontWeights.Bold);
            Run(editor, "s~~").TextDecorations.Should().BeEquivalentTo(TextDecorations.Strikethrough);
            Run(editor, "c]").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Paragraph.LinkForeground"));
            Run(editor, "d`").Typeface.FontFamily.Should().Be(editor.FindResource("Md.InlineCode.FontFamily"));
            Run(editor, "code").Typeface.FontFamily.Should().Be(editor.FindResource("Md.CodeBlock.FontFamily"));
            Run(editor, "## ").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Source.Markup.Foreground"));
            Run(editor, "](http").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Source.Markup.Foreground"));
            Run(editor, "[ ]").ForegroundBrush.Should().BeSameAs(Brushes.Purple);
            window.Close();
        });
    }

    private static int At(string piece) => Text.IndexOf(piece, StringComparison.Ordinal);

    private static MdSourceLook Describe(string text, params MdExtension[] extensions)
    {
        MdSourceLook? look = null;
        WpfHost.Invoke(() =>
        {
            var viewer = new LookViewer { Markdown = text };
            foreach (var extension in extensions)
            {
                viewer.Extensions.Add(extension);
            }

            WpfHost.Show(viewer).Close();
            look = viewer.Look();
        });

        return look!;
    }

    /// <summary>Свойства куска текста AvalonEdit, с которого начинается <paramref name="piece"/>.</summary>
    private static TextRunProperties Run(MarkdownEditor editor, string piece)
    {
        var view = editor.TextEditor.TextArea.TextView;
        var offset = At(piece);
        var line = view.GetOrConstructVisualLine(editor.TextEditor.Document.GetLineByOffset(offset));
        var column = offset - line.FirstDocumentLine.Offset;
        return line.Elements
            .First(element => element.RelativeTextOffset <= column && column < element.RelativeTextOffset + element.DocumentLength)
            .TextRunProperties;
    }

    /// <summary>Наследник вьюера, который открывает тестам описание исходника.</summary>
    private sealed class LookViewer : MarkdownViewer
    {
        public MdSourceLook Look() => GetSourceLook();
    }
}
