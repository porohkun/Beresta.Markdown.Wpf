namespace Beresta.Markdown.Wpf.Tests;

using Editor;
using Presenters;

/// <summary>Редактор из второго пакета.</summary>
public sealed class MarkdownEditorTests
{
    private const string Document = "# Title\n\nParagraph with **bold**.\n\n- one\n- [ ] two\n\n```cs\nvar a = 1;\n```\n\n> quote\n";

    [Fact]
    public void Без_режимов_рисует_документ_как_вьюер()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = Document };
            var editor = new MarkdownEditor { Markdown = Document };
            var viewerWindow = WpfHost.Show(viewer);
            var editorWindow = WpfHost.Show(editor);

            editor.Items.Count.Should().Be(viewer.Items.Count);
            editor.TextMap.Text.Should().Be(viewer.TextMap.Text);
            Visuals.Descendants<MdBlockItem>(editor).Count().Should().Be(Visuals.Descendants<MdBlockItem>(viewer).Count());
            viewerWindow.Close();
            editorWindow.Close();
        });
    }
}
