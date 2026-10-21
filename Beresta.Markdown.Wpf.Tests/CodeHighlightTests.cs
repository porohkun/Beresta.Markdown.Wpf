namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Editor;
using Extensions;
using ICSharpCode.AvalonEdit.Rendering;
using Markdig.Extensions.TaskLists;
using ViewModels;

/// <summary>
/// Подсветка кода: блок со знакомым языком режется на куски со стилями подсветки, а текст, выделение и
/// копирование не меняются; редактор подсвечивает определениями AvalonEdit и в отрисовке, и в исходнике.
/// </summary>
public sealed class CodeHighlightTests
{
    private const string Code = "```cs\nvar x = 1;\n```";

    [Fact]
    public void Блок_со_знакомым_языком_режется_на_подсвеченные_куски()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = Code, CodeHighlighter = new Words() };
            var window = WpfHost.Show(viewer);

            var code = viewer.Items.Cast<BlockVm>().OfType<CodeBlockVm>().Single();
            code.Runs.Cast<RunVm>().Select(run => (run.Text, run.StyleName)).Should().Equal(("var", "Code.Keyword"), (" x = ", null), ("1", "Code.Number"), (";", null));
            viewer.TextMap.Text.Should().Be("var x = 1;");
            viewer.SelectAll();
            viewer.SelectedText.Should().Be("var x = 1;");
            viewer.SelectedMarkdown.Should().Be(Code);
            window.Close();
        });
    }

    [Theory]
    [InlineData("```foo\nvar x = 1;\n```")]
    [InlineData("    var x = 1;")]
    public void Незнакомый_язык_и_код_без_языка_одним_куском(string markdown)
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = markdown, CodeHighlighter = new Words() };
            var window = WpfHost.Show(viewer);

            viewer.Items.Cast<BlockVm>().OfType<CodeBlockVm>().Single().Runs.Should().ContainSingle();
            window.Close();
        });
    }

    [Fact]
    public void Без_подсветчика_одним_куском()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = Code };
            var window = WpfHost.Show(viewer);

            viewer.Items.Cast<BlockVm>().OfType<CodeBlockVm>().Single().Runs.Should().ContainSingle();
            window.Close();
        });
    }

    [Fact]
    public void Неизменный_блок_не_подсвечивается_заново()
    {
        WpfHost.Invoke(() =>
        {
            var highlighter = new Words();
            var viewer = new MarkdownViewer { Markdown = Code, CodeHighlighter = highlighter };
            var window = WpfHost.Show(viewer);
            var runs = viewer.Items.Cast<BlockVm>().OfType<CodeBlockVm>().Single().Runs;

            viewer.Markdown = "A new paragraph\n\n" + Code;
            WpfHost.Settle(window);

            viewer.Items.Cast<BlockVm>().OfType<CodeBlockVm>().Single().Runs.Should().BeSameAs(runs);
            highlighter.Threads.Should().ContainSingle();
            window.Close();
        });
    }

    [Fact]
    public void Другой_подсветчик_подсвечивает_блок_заново()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = Code, CodeHighlighter = new Words() };
            var window = WpfHost.Show(viewer);
            var other = new Words();

            viewer.CodeHighlighter = other;
            WpfHost.Settle(window);

            other.Threads.Should().ContainSingle();
            viewer.CodeHighlighter = null;
            WpfHost.Settle(window);
            viewer.Items.Cast<BlockVm>().OfType<CodeBlockVm>().Single().Runs.Should().ContainSingle();
            window.Close();
        });
    }

    [Fact]
    public void Подсветчик_с_пометкой_работает_в_фоне_без_неё_в_потоке_интерфейса()
    {
        var safe = new SafeWords();
        var plain = new Words();
        WpfHost.Invoke(() =>
        {
            var left = new MarkdownViewer { Markdown = Code, CodeHighlighter = safe, BackgroundParseThreshold = 1 };
            var right = new MarkdownViewer { Markdown = Code, CodeHighlighter = plain, BackgroundParseThreshold = 1 };
            var window = WpfHost.Show(new StackPanel { Children = { left, right } });
            for (var attempt = 0; attempt < 300 && (left.TextMap.Text.Length == 0 || right.TextMap.Text.Length == 0); attempt++)
            {
                Thread.Sleep(10);
                WpfHost.Settle(window);
            }

            window.Close();
        });

        safe.Threads.Should().NotBeEmpty().And.NotContain(WpfHost.ThreadId);
        plain.Threads.Should().NotBeEmpty().And.OnlyContain(thread => thread == WpfHost.ThreadId);
    }

    [Fact]
    public void Сбой_подсветчика_уходит_в_RenderFailed_а_код_одним_куском()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { CodeHighlighter = new Broken() };
            var failures = new List<Exception>();
            viewer.RenderFailed += (_, e) => failures.Add(e.Exception);
            viewer.Markdown = Code;
            var window = WpfHost.Show(viewer);

            failures.Should().ContainSingle().Which.Message.Should().Be("broken");
            viewer.Items.Cast<BlockVm>().OfType<CodeBlockVm>().Single().Runs.Should().ContainSingle();
            window.Close();
        });
    }

    [Fact]
    public void Подсветчик_AvalonEdit_даёт_виды_кускам_кода()
    {
        const string code = "int n = 42; // note\nvar s = new string(\"text\");";
        const string script = "function f() { return 1; }";

        var pieces = MdAvalonCodeHighlighter.Instance.Highlight("cs", code)!
            .Select(span => (code.Substring(span.Start, span.Length), span.Look))
            .ToList();
        var scriptPieces = MdAvalonCodeHighlighter.Instance.Highlight("js", script)!
            .Select(span => (script.Substring(span.Start, span.Length), span.Look))
            .ToList();

        pieces.Should().Contain([("int", "Type"), ("42", "Number"), ("// note", "Comment"), ("new", "Keyword"), ("\"text\"", "String")]);
        scriptPieces.Should().Contain([("function", "Keyword"), ("return", "Keyword"), ("1", "Number")]);
        MdAvalonCodeHighlighter.Instance.Highlight("nolanguage", code).Should().BeNull();
    }

    [Fact]
    public void Редактор_подсвечивает_код_сам_и_в_исходнике()
    {
        WpfHost.Invoke(() =>
        {
            const string markdown = "```cs\nint n = 42; return n;\n```";
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Full };
            editor.Resources.MergedDictionaries.Add(new() { Source = new("pack://application:,,,/Beresta.Markdown.Wpf.Editor;component/Themes/Generic.xaml") });
            var window = WpfHost.Show(new ScrollViewer { Content = editor });

            editor.CodeHighlighter.Should().BeSameAs(MdAvalonCodeHighlighter.Instance);
            Run(editor, markdown, "return").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Code.Keyword.Foreground"));
            Run(editor, markdown, "42").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Code.Number.Foreground"));
            Run(editor, markdown, "int").ForegroundBrush.Should().BeSameAs(editor.FindResource("Md.Code.Type.Foreground"));
            window.Close();
        });
    }

    [Fact]
    public void Вид_без_своих_ключей_берёт_цвета_стиля()
    {
        WpfHost.Invoke(() =>
        {
            const string markdown = "- [ ] task";
            var tasks = new LambdaExtension
            {
                Inline = (inline, _) => inline is TaskList task ? new TaskVm(task.Checked) : null,
                Describe = (node, look) => look.Mark(node.Source.Start, node.Source.Length, "Probe")
            };
            var editor = new MarkdownEditor { Markdown = markdown, EditMode = MdEditMode.Full };
            editor.Extensions.Add(tasks);
            editor.Resources["Md.Probe.Brush"] = Brushes.Purple;
            editor.Resources["Md.Probe.Style"] = XamlReader.Parse("""
                <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="Span">
                    <Setter Property="Foreground" Value="{DynamicResource Md.Probe.Brush}" />
                    <Setter Property="TextDecorations" Value="Underline" />
                </Style>
                """);
            var window = WpfHost.Show(new ScrollViewer { Content = editor });

            var run = Run(editor, markdown, "[ ]");
            run.ForegroundBrush.Should().BeSameAs(Brushes.Purple);
            run.TextDecorations.Should().Contain(decoration => decoration.Location == TextDecorationLocation.Underline);
            window.Close();
        });
    }

    /// <summary>Свойства куска текста AvalonEdit, с которого начинается <paramref name="piece"/>.</summary>
    private static TextRunProperties Run(MarkdownEditor editor, string markdown, string piece)
    {
        var view = editor.TextEditor.TextArea.TextView;
        var offset = markdown.IndexOf(piece, StringComparison.Ordinal);
        VisualLine line = view.GetOrConstructVisualLine(editor.TextEditor.Document.GetLineByOffset(offset));
        var column = offset - line.FirstDocumentLine.Offset;
        return line.Elements
            .First(element => element.RelativeTextOffset <= column && column < element.RelativeTextOffset + element.DocumentLength)
            .TextRunProperties;
    }

    /// <summary>Подсветчик <c>cs</c>: <c>var</c> — ключевое слово, цифры — числа; где вызван — помнит.</summary>
    private class Words : IMdCodeHighlighter
    {
        public List<int> Threads { get; } = [];

        public IReadOnlyList<MdCodeSpan>? Highlight(string language, string code)
        {
            lock (Threads)
            {
                Threads.Add(Environment.CurrentManagedThreadId);
            }

            if (language != "cs")
            {
                return null;
            }

            var spans = new List<MdCodeSpan>();
            for (var at = code.IndexOf("var", StringComparison.Ordinal); at >= 0; at = code.IndexOf("var", at + 3, StringComparison.Ordinal))
            {
                spans.Add(new(at, 3, "Keyword"));
            }

            for (var i = 0; i < code.Length; i++)
            {
                if (char.IsDigit(code[i]))
                {
                    spans.Add(new(i, 1, "Number"));
                }
            }

            return spans;
        }
    }

    /// <summary>Тот же подсветчик, помеченный годным для фона.</summary>
    private sealed class SafeWords : Words, IMdBackgroundSafe;

    /// <summary>Подсветчик, который бросает.</summary>
    private sealed class Broken : IMdCodeHighlighter
    {
        public IReadOnlyList<MdCodeSpan>? Highlight(string language, string code) => throw new InvalidOperationException("broken");
    }
}
