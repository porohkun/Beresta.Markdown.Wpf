namespace Beresta.Markdown.Wpf.Tests;

using System.Windows.Controls;
using System.Windows.Input;
using Editor;
using Kit;
using Presenters;

/// <summary>Поля карточки свойств пишут значение в исходник тем же видом, каким оно было написано.</summary>
public sealed class FrontMatterEditTests
{
    [Theory]
    [InlineData("title: Hello", "World", "title: World")]
    [InlineData("title: Hello # note", "World", "title: World # note")]
    [InlineData("title:", "New", "title: New")]
    [InlineData("title: Hello", "a: b", "title: \"a: b\"")]
    [InlineData("title: Hello", "x #y", "title: \"x #y\"")]
    [InlineData("title: Hello", "-x", "title: \"-x\"")]
    [InlineData("title: Hello", " padded", "title: \" padded\"")]
    [InlineData("title: Hello", "true", "title: \"true\"")]
    [InlineData("title: Hello", "42", "title: \"42\"")]
    [InlineData("count: 1", "2", "count: 2")]
    [InlineData("title: 'Hello'", "It's", "title: 'It''s'")]
    [InlineData("title: \"Hello\"", "say \"hi\" \\o/", "title: \"say \\\"hi\\\" \\\\o/\"")]
    public void Поле_пишет_значение_тем_же_видом(string property, string value, string expected)
    {
        WpfHost.Invoke(() =>
        {
            var viewer = Card($"---\n{property}\nother: 1\n---\n\nText");
            var window = WpfHost.Show(viewer);
            var box = Visuals.Descendants<MdPropertyBox>(viewer).First();

            box.IsActive.Should().BeTrue();
            box.Text = value;
            box.Commit();
            WpfHost.Settle(window);

            viewer.Markdown.Should().Be($"---\n{expected}\nother: 1\n---\n\nText");
            window.Close();
        });
    }

    [Theory]
    [InlineData("author:\n  name: X")]
    [InlineData("text: |\n  line one\n  line two")]
    [InlineData("anchor: &a value")]
    [InlineData("map: {a: 1}")]
    public void Вложенное_и_многострочное_не_правится(string property)
    {
        WpfHost.Invoke(() =>
        {
            var viewer = Card($"---\n{property}\n---\n\nText");
            var window = WpfHost.Show(viewer);

            Visuals.Descendants<MdPropertyBox>(viewer).Single().IsActive.Should().BeFalse();
            window.Close();
        });
    }

    [Fact]
    public void Без_правки_front_matter_карточка_только_показывает()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = Card("---\ntitle: Hello\n---\n\nText");
            viewer.IsFrontMatterEditable = false;
            var window = WpfHost.Show(viewer);

            Visuals.Descendants<MdPropertyBox>(viewer).Single().IsActive.Should().BeFalse();
            window.Close();
        });
    }

    [Fact]
    public void Правка_поля_в_редакторе_отменяется_Ctrl_Z()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "---\ntitle: Hello\n---\n\nText", EditMode = MdEditMode.Controls };
            editor.Extensions.Add(new PropertiesCard());
            var window = WpfHost.Show(editor);
            var box = Visuals.Descendants<MdPropertyBox>(editor).Single();

            box.Text = "World";
            box.Commit();
            WpfHost.Settle(window);
            editor.Markdown.Should().Be("---\ntitle: World\n---\n\nText");

            ApplicationCommands.Undo.Execute(null, editor);
            WpfHost.Settle(window);
            editor.Markdown.Should().Be("---\ntitle: Hello\n---\n\nText");
            window.Close();
        });
    }

    [Theory]
    [InlineData(MdFrontMatterEditing.Text, 1, false)]
    [InlineData(MdFrontMatterEditing.Fields, 4, true)]
    [InlineData(MdFrontMatterEditing.None, 4, false)]
    public void Полный_режим_и_поля_по_способу_правки_front_matter(MdFrontMatterEditing editing, int first, bool fields)
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "---\ntitle: Hello\n---\n\nText\n\nMore", EditMode = MdEditMode.Full, FrontMatterEditing = editing };
            editor.Extensions.Add(new PropertiesCard());
            var window = WpfHost.Show(new ScrollViewer { Content = editor });

            editor.WindowLines.Should().Be((first, 7));
            editor.IsFrontMatterEditable.Should().Be(fields);
            window.Close();
        });
    }

    private static MarkdownViewer Card(string markdown)
    {
        var viewer = new MarkdownViewer { Markdown = markdown, AreControlsEditable = true, IsFrontMatterEditable = true };
        viewer.Extensions.Add(new PropertiesCard());
        return viewer;
    }
}
