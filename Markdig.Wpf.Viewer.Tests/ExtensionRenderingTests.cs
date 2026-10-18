namespace MarkdigWpfViewer.Tests;

using System.Windows.Controls;
using Extensions;
using Presenters;
using ViewModels;
using static TestWidgets;

/// <summary>Виджеты расширений рисуются их шаблонами, вложенная разметка — как обычно.</summary>
public sealed class ExtensionRenderingTests
{
    [Fact]
    public void Виджет_рисуется_шаблоном_расширения_и_вложенная_разметка_тоже()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Extensions = { Note() }, Markdown = ":::note T\nвнутри **жирное**\n:::" };
            var window = WpfHost.Show(viewer);

            var note = Visuals.Descendants<Border>(viewer).Single(border => Equals(border.Tag, "note"));
            TextOf(Visuals.Descendants<MdTextBlock>(note).Single()).Should().Be("внутри жирное");
            window.Close();
        });
    }

    [Fact]
    public void Строчный_виджет_рисуется_внутри_текста()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Extensions = { Mention() }, Markdown = "привет @ivan!" };
            var window = WpfHost.Show(viewer);

            var text = Visuals.Descendants<MdTextBlock>(viewer).Single();
            Visuals.Descendants<TextBlock>(text).Should().Contain(block => Equals(block.Tag, "mention") && block.Text == "ivan");
            window.Close();
        });
    }

    [Fact]
    public void Добавленное_позже_расширение_пересобирает_документ()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ":::note T\nвнутри\n:::" };
            var window = WpfHost.Show(viewer);
            viewer.Items.OfType<NoteVm>().Should().BeEmpty();

            viewer.Extensions.Add(Note());
            WpfHost.Settle(window);

            viewer.Items.OfType<NoteVm>().Should().ContainSingle();
            window.Close();
        });
    }

    [Fact]
    public void Общий_набор_расширений_работает_в_нескольких_вьюерах()
    {
        WpfHost.Invoke(() =>
        {
            MdExtensionCollection shared = [Note()];
            var first = new MarkdownViewer { SharedExtensions = shared, Markdown = ":::note A\na\n:::" };
            var second = new MarkdownViewer { SharedExtensions = shared, Markdown = ":::note B\nb\n:::" };
            var panel = new StackPanel { Children = { first, second } };
            var window = WpfHost.Show(panel);

            first.Items.OfType<NoteVm>().Should().ContainSingle();
            second.Items.OfType<NoteVm>().Should().ContainSingle();
            window.Close();
        });
    }

    private static string TextOf(MdTextBlock block)
        => ModelDump.Text(block.Runs ?? Array.Empty<InlineVm>());
}