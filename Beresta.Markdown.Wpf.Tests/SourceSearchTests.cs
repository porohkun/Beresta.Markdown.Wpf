namespace Beresta.Markdown.Wpf.Tests;

using System.Diagnostics;
using System.Windows.Controls;
using Editor;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>
/// Поиск и замена в исходнике редактора: совпадения — в документе, видны в окне и в отрисовке, заменяются по
/// одному и все сразу одним шагом отмены; после правки ищутся заново, когда правки на миг стихнут.
/// </summary>
public sealed class SourceSearchTests
{
    private const string Text = "Find me here.\n\nAnd [me](https://me.org) too.\n\n```\nme\n```";

    [Fact]
    public void Совпадения_в_исходнике_а_пометки_в_нарисованном()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show(Text, MdEditMode.View);
            using var search = new MdSourceSearch(editor) { Query = "me" };

            search.Matches.Select(match => Text.Substring(match.Start, match.Length)).Should().Equal("me", "me", "me", "me");
            search.Matches.Select(match => match.StartLine).Should().Equal(0, 2, 2, 5);
            search.Layer.Marks.Where(mark => mark.Style == "Found")
                .Select(mark => editor.TextMap.Text.Substring(mark.Range!.Value.Start, mark.Range!.Value.Length))
                .Should().Equal("me", "me", "me");
            search.Layer.Marks.Should().ContainSingle(mark => mark.Style == "Current");
            editor.TextEditor.TextArea.TextView.BackgroundRenderers.Should().Contain(renderer => renderer.Layer == KnownLayer.Selection);
            window.Close();
        });
    }

    [Fact]
    public void Дальше_и_назад_в_полном_режиме_выделяют_совпадение()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show(Text, MdEditMode.Full);
            using var search = new MdSourceSearch(editor) { Query = "me" };

            search.Next().Should().BeTrue();
            (editor.SourceSelectionStart, editor.SourceSelectionLength).Should().Be((search.Current!.Value.Start, 2));
            search.CurrentIndex.Should().Be(1);
            search.Previous();
            search.Previous();
            search.CurrentIndex.Should().Be(3);
            editor.SourceSelectionStart.Should().Be(search.Current!.Value.Start);
            window.Close();
        });
    }

    [Fact]
    public void Замена_текущего_переходит_к_следующему()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show("a me b me c", MdEditMode.Full);
            using var search = new MdSourceSearch(editor) { Query = "me" };

            search.Replace("you").Should().BeTrue();

            editor.TextEditor.Text.Should().Be("a you b me c");
            search.Count.Should().Be(1);
            search.Current!.Value.Start.Should().Be(8);
            window.Close();
        });
    }

    [Fact]
    public void Замена_всех_отменяется_одним_шагом()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show(Text, MdEditMode.Controls);
            using var search = new MdSourceSearch(editor) { Query = "me" };

            search.ReplaceAll("you").Should().Be(4);
            editor.TextEditor.Text.Should().Be("Find you here.\n\nAnd [you](https://you.org) too.\n\n```\nyou\n```");
            search.Count.Should().Be(0);

            editor.Undo();
            Pause(window);
            editor.TextEditor.Text.Should().Be(Text);
            search.Count.Should().Be(4);
            window.Close();
        });
    }

    [Fact]
    public void Совпадение_в_ссылке_не_нарисовано_но_заменяется()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show("[text](https://me.org)", MdEditMode.Controls);
            using var search = new MdSourceSearch(editor) { Query = "me.org" };

            search.Count.Should().Be(1);
            search.Layer.Marks.Should().BeEmpty();
            search.ReplaceAll("you.org");

            editor.TextEditor.Text.Should().Be("[text](https://you.org)");
            window.Close();
        });
    }

    [Fact]
    public void Совпадения_обновляются_после_набора()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show("one two", MdEditMode.Full);
            using var search = new MdSourceSearch(editor) { Query = "two" };
            editor.SelectSource(0, 0);

            editor.TextEditor.TextArea.PerformTextInput("two ");
            search.Matches.Select(match => match.Start).Should().Equal(8);
            Pause(window);
            search.Matches.Select(match => match.Start).Should().Equal(0, 8);
            editor.CommitText();
            WpfHost.Settle(window);

            search.Layer.Marks.Where(mark => mark.Style == "Found").Should().HaveCount(2);
            window.Close();
        });
    }

    [Fact]
    public void В_просмотре_замены_нет()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show("a me b", MdEditMode.View);
            using var search = new MdSourceSearch(editor) { Query = "me" };

            search.Replace("you").Should().BeFalse();
            search.ReplaceAll("you").Should().Be(0);

            editor.TextEditor.Text.Should().Be("a me b");
            window.Close();
        });
    }

    [Fact]
    public void Без_поиска_слой_и_фон_убираются()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show(Text, MdEditMode.View);
            var renderers = editor.TextEditor.TextArea.TextView.BackgroundRenderers.Count;
            var search = new MdSourceSearch(editor) { Query = "me" };

            search.Dispose();

            editor.MarkLayers.Should().NotContain(search.Layer);
            editor.TextEditor.TextArea.TextView.BackgroundRenderers.Should().HaveCount(renderers);
            window.Close();
        });
    }

    [Fact]
    public void Совпадение_внутри_правки_уходит_до_нового_поиска()
    {
        WpfHost.Invoke(() =>
        {
            var (editor, window) = Show("one two two", MdEditMode.Full);
            using var search = new MdSourceSearch(editor) { Query = "two" };
            editor.SelectSource(5, 1);

            editor.TextEditor.TextArea.PerformTextInput("x");
            search.Matches.Select(match => match.Start).Should().Equal(8);
            Pause(window);
            search.Matches.Select(match => match.Start).Should().Equal(8);
            window.Close();
        });
    }

    /// <summary>Правки стихли: поиск успевает найти заново.</summary>
    private static void Pause(System.Windows.Window window)
    {
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < 400)
        {
            WpfHost.Settle(window);
            Thread.Sleep(10);
        }
    }

    private static (MarkdownEditor Editor, System.Windows.Window Window) Show(string markdown, MdEditMode mode)
    {
        var editor = new MarkdownEditor { Markdown = markdown, EditMode = mode };
        var window = WpfHost.Show(new ScrollViewer { Content = editor });
        return (editor, window);
    }
}
