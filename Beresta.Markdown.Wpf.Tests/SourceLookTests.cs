namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Editor;
using Extensions;
using ICSharpCode.AvalonEdit.Rendering;
using Markdig.Extensions.CustomContainers;
using Markdig.Extensions.TaskLists;
using Selection;
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
        look.Lines.Should().Contain([(4, 4, "Quote"), (8, 10, "CodeBlock"), (1, 1, "BlankLine"), (11, 11, "BlankLine")]);
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
    public void Вложенное_своей_модели_описано_как_встроенное()
    {
        const string text = ":::note Title\nA **b** c\n:::";
        var notes = new LambdaExtension
        {
            Block = (block, context) => block is CustomContainer { Info: "note" } container ? new NoteVm(context.ConvertChildren(container)) : null
        };

        var look = Describe(text, notes);

        look.Pieces.Should().Contain((text.IndexOf("b**", StringComparison.Ordinal), 1, "Bold"));
        look.Pieces
            .Where(piece => piece.Look == "Source.Markup")
            .Select(piece => text.Substring(piece.Start, piece.Length).Trim())
            .Should().BeEquivalentTo([":::note Title", "**", "**", ":::"]);
    }

    [Fact]
    public void Своя_модель_без_вложенного_с_местом_остаётся_простым_текстом()
    {
        const string text = ":::card Title\nA **b** c\n:::\n\n:::broken\nA **b** c\n:::";
        var widgets = new LambdaExtension
        {
            Block = (block, context) => block switch
            {
                CustomContainer { Info: "card" } => new CardVm(),
                CustomContainer { Info: "broken" } container => new BrokenVm(context.ConvertChildren(container)),
                _ => null
            }
        };

        var look = Describe(text, widgets);

        look.Pieces.Should().BeEmpty();
        look.Lines.Should().Equal((3, 3, "BlankLine"));
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

    [Fact]
    public void Выделенный_кусок_без_своего_фона_и_цветом_выделения()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = Text, EditMode = MdEditMode.Full };
            var window = WpfHost.Show(new ScrollViewer { Content = editor });
            Run(editor, "d`").BackgroundBrush.Should().NotBeNull();

            editor.SelectSource(At("d`"), 1);
            WpfHost.Settle(window);

            Run(editor, "d`").BackgroundBrush.Should().BeNull();
            Run(editor, "d`").ForegroundBrush.Should().BeSameAs(editor.TextEditor.TextArea.SelectionForeground);
            Run(editor, "code").BackgroundBrush.Should().NotBeNull();
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

    /// <summary>Своя модель-контейнер: вложенные блоки пишет в карту.</summary>
    private sealed record NoteVm(IReadOnlyList<BlockVm> Blocks) : BlockVm, IMdTextSource
    {
        public void WriteText(MdTextWriter writer) => writer.Blocks(Blocks);
    }

    /// <summary>Своя модель-лист: пишет в карту только свой текст.</summary>
    private sealed record CardVm : BlockVm, IMdTextSource
    {
        public void WriteText(MdTextWriter writer) => writer.Text("card");
    }

    /// <summary>Своя модель, которая падает на записи в карту.</summary>
    private sealed record BrokenVm(IReadOnlyList<BlockVm> Blocks) : BlockVm, IMdTextSource
    {
        public void WriteText(MdTextWriter writer) => throw new InvalidOperationException("broken");
    }

    /// <summary>Наследник вьюера, который открывает тестам описание исходника.</summary>
    private sealed class LookViewer : MarkdownViewer
    {
        public MdSourceLook Look() => GetSourceLook();
    }
}
