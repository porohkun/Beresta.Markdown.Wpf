namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Editor;
using Extensions;
using Kit;

/// <summary>
/// Готовые расширения в исходнике: плашки — полосой и видом в заголовке цвета своего вида, вики-ссылки,
/// теги, упоминания, CriticMarkup, чипы и спойлеры — видами и цветами, как в отрисовке.
/// </summary>
public sealed class KitSourceLookTests
{
    private const string Text =
        "> [!WARNING]\n> Careful\n\n"
        + "> [!faq]- Question\n> Answer\n\n"
        + "!!! tip \"Hint\"\n    Body\n\n"
        + ":::danger Stop\nDanger body\n:::\n\n"
        + "See [[Page]], [[Page#Part|caption]], [[Other#Part]], ![[note]] and ![[pic.png|200]].\n\n"
        + "A #tag, @anna, ::chip::, ::done::{.success} and ||secret||.\n\n"
        + "{++ins++} {--del--} {~~old~>new~~} {==mark==} {>>remark<<}";

    [Fact]
    public void Плашка_полосой_и_видом_в_заголовке_цвета_своего_вида()
    {
        var look = Describe();

        look.Lines.Should().Contain([(0, 1, "Callout.Warning"), (3, 4, "Callout.Question"), (6, 7, "Callout.Tip"), (9, 11, "Callout.Danger")]);
        look.Pieces.Should().Contain([
            Piece("[!WARNING]", "Callout.Warning"),
            Piece("[!faq]-", "Callout.Question"),
            Piece("tip \"Hint\"", "Callout.Tip", 3),
            Piece("danger Stop", "Callout.Danger", 6)]);
        look.Indents.Should().Contain([(0, 2), (1, 2), (3, 2), (4, 2)]);
    }

    [Fact]
    public void Вики_ссылка_скобки_раздел_и_страница_при_подписи_знаками_разметки()
    {
        var look = Describe();

        look.Pieces.Should().Contain([
            Piece("Page]]", "WikiLink", 4),
            Piece("caption", "Link"),
            Piece("caption", "WikiLink"),
            Piece("[[Page#Part|", "Source.Markup"),
            Piece("#Part]]", "Source.Markup", 1),
            Piece("![[note", "Source.Markup", 3),
            Piece("![[pic", "Source.Markup", 3),
            Piece("|200]]", "Source.Markup")]);
        Covered(look, "note]]", 4, "Link").Should().BeTrue();
        Markup(look).Should().Contain(["[[", "]]", "[[Page#Part|", "#", "![[", "|200]]"]);
        Markup(look).Should().NotContain(piece => piece.Contains("Page]") || piece.Contains("caption") || piece.Contains("Other"));
    }

    [Fact]
    public void Тег_упоминание_чип_и_спойлер_своими_видами()
    {
        var look = Describe();

        look.Pieces.Should().Contain([
            Piece("#tag", "Link"),
            Piece("#tag", "Tag"),
            Piece("@anna", "Link"),
            Piece("@anna", "Mention"),
            Piece("chip::", "Chip", 4),
            Piece("done::", "Chip.Success", 4),
            Piece("secret", "Spoiler")]);
        Markup(look).Should().Contain(["::", "||"]);
    }

    [Fact]
    public void Правки_CriticMarkup_оформленным_текстом_а_комментарий_знаками_разметки()
    {
        var look = Describe();

        look.Pieces.Should().Contain([
            Piece("ins++", "Critic.Insertion", 3),
            Piece("del--", "Critic.Deletion", 3),
            Piece("old~>", "Critic.Deletion", 3),
            Piece("new~~", "Critic.Insertion", 3),
            Piece("mark==", "Critic.Highlight", 4),
            Piece("{>>remark<<}", "Source.Markup")]);
        Markup(look).Should().Contain(["{++", "++}", "{--", "--}", "{~~", "~>", "~~}", "{==", "==}"]);
        Markup(look).Should().NotContain(piece => piece.Contains("ins") || piece.Contains("old") || piece.Contains("new"));
    }

    [Fact]
    public void Редактор_красит_исходник_расширений_цветами_темы()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = Text, EditMode = MdEditMode.Full };
            AddExtensions(editor);
            editor.Resources.MergedDictionaries.Add(new() { Source = new("pack://application:,,,/Beresta.Markdown.Wpf.Editor;component/Themes/Generic.xaml") });
            var window = WpfHost.Show(new ScrollViewer { Content = editor });

            Run(editor, "[!WARNING]").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Callout.Warning.Brush"));
            Run(editor, "tip \"Hint\"").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Callout.Tip.Brush"));
            Run(editor, "Question\n").ForegroundBrush.Should().NotBeSameAs(editor.FindResource("Md.Source.Markup.Foreground"));
            Run(editor, "caption").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Paragraph.LinkForeground"));
            Run(editor, "[[Page#").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Source.Markup.Foreground"));
            Run(editor, "#tag").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Paragraph.LinkForeground"));
            Run(editor, "#tag").BackgroundBrush.Should().BeSameAs(editor.FindResource("Md.Mention.Background"));
            Run(editor, "@anna").BackgroundBrush.Should().BeSameAs(editor.FindResource("Md.Mention.Background"));
            Run(editor, "chip::").BackgroundBrush.Should().BeSameAs(editor.FindResource("Md.Chip.Background"));
            Run(editor, "done::").BackgroundBrush.Should().BeSameAs(editor.FindResource("Md.Chip.Success.Background"));
            Run(editor, "done::").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Callout.Success.Brush"));
            Run(editor, "secret").BackgroundBrush.Should().BeSameAs(editor.FindResource("Md.Chip.Background"));
            Run(editor, "ins++").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Mark.Added.LineBrush"));
            Run(editor, "ins++").TextDecorations.Should().Contain(decoration => decoration.Location == TextDecorationLocation.Underline);
            Run(editor, "del--").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Mark.Removed.LineBrush"));
            Run(editor, "del--").TextDecorations.Should().Contain(decoration => decoration.Location == TextDecorationLocation.Strikethrough);
            Run(editor, "mark==").BackgroundBrush.Should().BeSameAs(editor.FindResource("Md.Marked.Background"));
            Run(editor, "{>>").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Source.Markup.Foreground"));
            window.Close();
        });
    }

    [Fact]
    public void Плашка_без_своей_кисти_как_заметка()
    {
        WpfHost.Invoke(() =>
        {
            var element = new Border();
            var find = (string key) => element.TryFindResource(key);

            var known = LookStyle.Resolve("Callout.Warning", find);
            var unknown = LookStyle.Resolve("Callout.Foo", find);

            known.Bar.Should().BeSameAs(element.FindResource("Md.Callout.Warning.Brush"));
            known.Foreground.Should().BeSameAs(known.Bar);
            known.BarWidth.Should().Be(((Thickness)element.FindResource("Md.Callout.BorderThickness")).Left);
            unknown.Bar.Should().BeSameAs(element.FindResource("Md.Callout.Note.Brush"));
        });
    }

    private static (int Start, int Length, string Look) Piece(string piece, string look, int? length = null)
        => (Text.IndexOf(piece, StringComparison.Ordinal), length ?? piece.Length, look);

    /// <summary>Кусок <paramref name="piece"/> длиной <paramref name="length"/> лежит внутри куска вида <paramref name="name"/>.</summary>
    private static bool Covered(MdSourceLook look, string piece, int length, string name)
    {
        var start = Text.IndexOf(piece, StringComparison.Ordinal);
        return look.Pieces.Any(covering => covering.Look == name && covering.Start <= start && start + length <= covering.Start + covering.Length);
    }

    /// <summary>Тексты кусков знаков разметки.</summary>
    private static List<string> Markup(MdSourceLook look)
        => look.Pieces
            .Where(piece => piece.Look == "Source.Markup")
            .Select(piece => Text.Substring(piece.Start, piece.Length).Trim())
            .ToList();

    private static void AddExtensions(MarkdownViewer viewer)
    {
        MdExtension[] extensions =
        [
            new ObsidianCallouts(), new MkDocsAdmonitions(), new CalloutContainers(), new WikiLinks(), new WikiEmbeds(),
            new Tags(), new Mentions(), new Chips(), new Spoilers(), new CriticMarkup()
        ];
        foreach (var extension in extensions)
        {
            viewer.Extensions.Add(extension);
        }
    }

    private static MdSourceLook Describe()
    {
        MdSourceLook? look = null;
        WpfHost.Invoke(() =>
        {
            var viewer = new LookViewer { Markdown = Text };
            AddExtensions(viewer);
            WpfHost.Show(viewer).Close();
            look = viewer.Look();
        });

        return look!;
    }

    /// <summary>Свойства куска текста AvalonEdit, с которого начинается <paramref name="piece"/>.</summary>
    private static TextRunProperties Run(MarkdownEditor editor, string piece)
    {
        var view = editor.TextEditor.TextArea.TextView;
        var offset = Text.IndexOf(piece, StringComparison.Ordinal);
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
