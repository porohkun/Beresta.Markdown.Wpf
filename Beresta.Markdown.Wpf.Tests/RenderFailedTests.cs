namespace Beresta.Markdown.Wpf.Tests;

using Extensions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Presenters;
using Selection;
using ViewModels;

/// <summary>Упавший код расширения или Markdig не роняет вьюер: узел достаётся встроенному разбору, сбой — событию.</summary>
public sealed class RenderFailedTests
{
    [Fact]
    public void Упавший_разбор_блока_отдаёт_узел_встроенному_разбору()
    {
        var failing = new LambdaExtension { Block = (block, _) => block is ParagraphBlock ? throw new InvalidOperationException("block") : null };

        var (text, failures) = Show("text **bold**", failing);

        text.Should().Be("text bold");
        failures.Should().ContainSingle().Which.Should().Match<MdRenderFailedEventArgs>(failure =>
            failure.Extension == failing && failure.Exception.Message == "block" && !failure.IsBackground);
    }

    [Fact]
    public void Упавший_разбор_инлайна_отдаёт_узел_встроенному_разбору()
    {
        var failing = new LambdaExtension { Inline = (inline, _) => inline is EmphasisInline ? throw new InvalidOperationException("inline") : null };

        var (text, failures) = Show("text **bold**", failing);

        text.Should().Be("text bold");
        failures.Should().ContainSingle().Which.Should().Match<MdRenderFailedEventArgs>(failure =>
            failure.Extension == failing && failure.Exception.Message == "inline");
    }

    [Fact]
    public void Упавшая_настройка_разбора_не_оставляет_своих_расширений_Markdig()
    {
        var failing = new LambdaExtension
        {
            Setup = pipeline =>
            {
                pipeline.UseEmojiAndSmiley();
                throw new InvalidOperationException("setup");
            }
        };

        var (text, failures) = Show("hello :smile:", failing);

        text.Should().Be("hello :smile:");
        failures.Should().ContainSingle().Which.Should().Match<MdRenderFailedEventArgs>(failure =>
            failure.Extension == failing && failure.Exception.Message == "setup");
    }

    [Fact]
    public void Упавший_выбор_шаблона_отдаёт_блок_шаблону_темы()
    {
        var failing = new LambdaExtension { Template = _ => throw new InvalidOperationException("template") };
        var drawn = false;

        var (_, failures) = Show("text", failing, viewer => drawn = Visuals.Descendants<MdTextBlock>(viewer).Any());

        drawn.Should().BeTrue();
        failures.Should().NotBeEmpty().And.OnlyContain(failure => failure.Extension == failing && failure.Exception.Message == "template");
    }

    [Fact]
    public void Упавшее_состояние_виджета_не_мешает_показу()
    {
        var failing = new LambdaExtension { State = _ => throw new InvalidOperationException("state") };

        var (text, failures) = Show("text", failing);

        text.Should().Be("text");
        failures.Should().NotBeEmpty().And.OnlyContain(failure => failure.Extension == failing && failure.Exception.Message == "state");
    }

    [Fact]
    public void Виджет_с_упавшим_текстом_становится_неделимым()
    {
        var failing = new LambdaExtension { Block = (block, _) => block is ParagraphBlock ? new BrokenTextVm() : null };
        var markdown = string.Empty;

        var (text, failures) = Show(
            "text",
            failing,
            viewer =>
            {
                viewer.SelectAll();
                markdown = viewer.SelectedMarkdown;
            });

        text.Should().Be(MdTextMap.ObjectMark.ToString());
        markdown.Should().Be("text");
        failures.Should().ContainSingle().Which.Should().Match<MdRenderFailedEventArgs>(failure =>
            failure.Extension == failing && failure.Exception.Message == "text");
    }

    [Fact]
    public void Кусок_виджета_с_упавшим_текстом_копируется_исходником_виджета()
    {
        var failing = new LambdaExtension { Block = (block, _) => block is ParagraphBlock ? new FlakyTextVm() : null };
        var markdown = string.Empty;

        var (text, failures) = Show(
            "text",
            failing,
            viewer =>
            {
                viewer.Select(1, 1);
                markdown = viewer.SelectedMarkdown;
            });

        text.Should().Be("abc");
        markdown.Should().Be("text");
        failures.Should().ContainSingle().Which.Should().Match<MdRenderFailedEventArgs>(failure =>
            failure.Extension == failing && failure.Exception.Message == "again");
    }

    [Fact]
    public void Вложенность_которую_Markdig_не_разбирает_показывается_строками_исходника()
    {
        var line = string.Concat(Enumerable.Repeat("- ", 150)) + "deep";

        var (text, failures) = Show($"{line}\n\nnext", null);

        text.Should().Be($"{line}\n\nnext");
        failures.Should().ContainSingle().Which.Should().Match<MdRenderFailedEventArgs>(failure =>
            failure.Extension == null && failure.Exception is ArgumentException);
    }

    [Fact]
    public void Запасной_документ_собирает_абзацы_из_непустых_строк()
    {
        ModelDump.Of(ModelBuilder.SourceDocument("\na\n> b\n\n \nc\n\n")).Should().Be("P(a\n> b) _ _ P(c)");
    }

    /// <summary>Показывает текст во вьюере с расширением; отдаёт текст карты и сбои, пришедшие в событие.</summary>
    private static (string Text, List<MdRenderFailedEventArgs> Failures) Show(string markdown, MdExtension? extension, Action<MarkdownViewer>? act = null)
    {
        var text = string.Empty;
        var failures = new List<MdRenderFailedEventArgs>();
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer();
            if (extension is not null)
            {
                viewer.Extensions.Add(extension);
            }

            viewer.RenderFailed += (_, e) => failures.Add(e);
            var window = WpfHost.Show(viewer);

            viewer.Markdown = markdown;
            WpfHost.Settle(window);
            act?.Invoke(viewer);
            WpfHost.Settle(window);

            text = viewer.TextMap.Text;
            window.Close();
        });

        return (text, failures);
    }

    /// <summary>Виджет, который не может написать свой текст.</summary>
    private sealed record BrokenTextVm : BlockVm, IMdTextSource
    {
        public void WriteText(MdTextWriter writer) => throw new InvalidOperationException("text");
    }

    /// <summary>Виджет, который пишет свой текст только в первый раз.</summary>
    private sealed record FlakyTextVm : BlockVm, IMdTextSource
    {
        private int _calls;

        public void WriteText(MdTextWriter writer)
        {
            if (_calls++ > 0)
            {
                throw new InvalidOperationException("again");
            }

            writer.Text("abc");
        }
    }
}
