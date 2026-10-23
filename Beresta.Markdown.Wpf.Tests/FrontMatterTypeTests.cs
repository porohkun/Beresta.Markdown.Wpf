namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Editor;
using Kit;
using Presenters;

/// <summary>
/// Карточка свойств по типам: флаг — флажком, дата — выбором даты, список — чипами, а свойства
/// добавляются и удаляются; тип — по полю приложения или по значению.
/// </summary>
public sealed class FrontMatterTypeTests
{
    [Theory]
    [InlineData("flag: true", "Bool")]
    [InlineData("flag: Off", "Bool")]
    [InlineData("flag: 'true'", "Text")]
    [InlineData("date: 2024-05-06", "Date")]
    [InlineData("date: '2024-05-06'", "Date")]
    [InlineData("date: 2024-13-06", "Text")]
    [InlineData("tags: [a, b]", "List")]
    [InlineData("tags:\n  - a\n  - b", "List")]
    [InlineData("title: Hello", "Text")]
    [InlineData("count: 1", "Text")]
    [InlineData("author:\n  name: X", "Text")]
    public void Тип_угадывается_по_значению(string property, string type)
        => Rows($"---\n{property}\n---\n\nText").Single().Type.Should().Be(new MdPropertyType(type));

    [Fact]
    public void Поле_приложения_сильнее_догадки()
    {
        var rows = Rows(
            "---\ntags: one\ndone: maybe\ncount: true\nauthor:\n  name: X\n---\n\nText",
            new() { Key = "tags", Type = MdPropertyType.List },
            new() { Key = "done", Type = MdPropertyType.Bool },
            new() { Key = "count", Type = MdPropertyType.Text },
            new() { Key = "author", Type = MdPropertyType.Bool });

        rows.Select(row => row.Type).Should().Equal(MdPropertyType.List, MdPropertyType.Bool, MdPropertyType.Text, MdPropertyType.Text);
        rows[0].Items.Should().Equal("one");
        rows[1].IsChecked.Should().BeFalse();
    }

    [Fact]
    public void Значения_по_типам_и_ключи_для_добавления()
    {
        PropertiesVm? model = null;
        WpfHost.Invoke(() =>
        {
            var card = Card("---\nflag: yes\ndate: 2024-05-06\ntags: [a, b]\n---\n\nText", new() { Key = "flag" }, new() { Key = "author" }, new() { Key = "date" });
            WpfHost.Show(card).Close();
            model = card.Items.OfType<PropertiesVm>().Single();
        });

        model!.Items[0].Type.Should().Be(MdPropertyType.Bool);
        model.Items[0].IsChecked.Should().BeTrue();
        model.Items[1].Date.Should().Be(new DateTime(2024, 5, 6));
        model.Items[2].Items.Should().Equal("a", "b");
        model.Suggestions.Should().Equal("author");
    }

    [Fact]
    public void Тип_из_XAML_по_имени()
    {
        var fields = (PropertiesCard)System.Windows.Markup.XamlReader.Parse("""
            <md:PropertiesCard xmlns:md="https://github.com/porohkun/Beresta.Markdown.Wpf">
                <md:PropertiesCard.Fields>
                    <md:MdPropertyField Key="done" Type="Bool" />
                    <md:MdPropertyField Key="rating" Type="Rating" />
                </md:PropertiesCard.Fields>
            </md:PropertiesCard>
            """);

        fields.Fields.Select(field => field.Type).Should().Equal(MdPropertyType.Bool, new MdPropertyType("Rating"));
    }

    [Theory]
    [InlineData("flag: true", false, "flag: false")]
    [InlineData("flag: false", true, "flag: true")]
    [InlineData("draft: no", true, "draft: yes")]
    [InlineData("draft: On", false, "draft: Off")]
    [InlineData("draft: TRUE", false, "draft: FALSE")]
    public void Флажок_пишет_флаг_словом_прежнего(string property, bool value, string expected)
        => Edit(property, expected, viewer => Visuals.Descendants<CheckBox>(viewer).Single().IsChecked = value);

    [Theory]
    [InlineData("date: 2024-05-06")]
    [InlineData("date: '2024-05-06'")]
    [InlineData("date: \"2024-05-06\" # comment")]
    public void Дата_пишется_без_кавычек(string property)
        => Edit(property, property.Contains('#') ? "date: 2025-01-02 # comment" : "date: 2025-01-02", viewer => Visuals.Descendants<MdPropertyDatePicker>(viewer).Single().SelectedDate = new DateTime(2025, 1, 2));

    [Theory]
    [InlineData("---\ntags: [\"Doe, John\", Smith]\n---\n")]
    [InlineData("---\ntags:\n  - Doe, John\n  - Smith\n---\n")]
    public void Пункт_с_запятой_остаётся_одним_пунктом(string markdown)
        => Rows(markdown).Single().Items.Should().Equal("Doe, John", "Smith");

    [Fact]
    public void Крестик_чипа_убирает_свой_пункт_из_одинаковых()
        => Edit("tags: [a, b, a]", "tags: [a, b]", viewer => MdPropertiesPresenter.RemoveItemCommand.Execute(
            "a",
            Visuals.Descendants<Button>(viewer).Last(button => button.Command == MdPropertiesPresenter.RemoveItemCommand && Equals(button.CommandParameter, "a"))));

    [Theory]
    [InlineData("tags: [a, b]", "a", "tags: [b]")]
    [InlineData("tags: [a, b]", "b", "tags: [a]")]
    [InlineData("tags:\n  - a\n  - b", "a", "tags:\n  - b")]
    [InlineData("tags:\n- a\n- b", "b", "tags:\n- a")]
    public void Крестик_чипа_убирает_пункт(string property, string item, string expected)
        => Edit(property, expected, viewer => MdPropertiesPresenter.RemoveItemCommand.Execute(
            item,
            Visuals.Descendants<Button>(viewer).Single(button => button.Command == MdPropertiesPresenter.RemoveItemCommand && Equals(button.CommandParameter, item))));

    [Theory]
    [InlineData("tags: [a, b]", "c", "tags: [a, b, c]")]
    [InlineData("tags: [a, b]", "x: y", "tags: [a, b, \"x: y\"]")]
    [InlineData("tags:\n  - a\n  - b", "c", "tags:\n  - a\n  - b\n  - c")]
    [InlineData("tags:\n- a", "c", "tags:\n- a\n- c")]
    [InlineData("tags: []", "c", "tags: [c]")]
    [InlineData("tags: [\"Doe, John\", b]", "c", "tags: [\"Doe, John\", b, c]")]
    [InlineData("tags:\n  - Doe, John\n  - b", "c", "tags:\n  - Doe, John\n  - b\n  - c")]
    public void Поле_чипов_добавляет_пункт(string property, string item, string expected)
        => Edit(property, expected, viewer =>
        {
            var field = Visuals.Descendants<TextBox>(viewer).Single(box => box.InputBindings.OfType<KeyBinding>().Any(binding => binding.Command == MdPropertiesPresenter.AddItemCommand));
            field.Text = item;
            MdPropertiesPresenter.AddItemCommand.Execute(item, field);
            field.Text.Should().BeEmpty();
        });

    [Theory]
    [InlineData("---\n\n---\n\nText", "title", "---\n\ntitle: \n---\n\nText")]
    [InlineData("---\na: 1\n---\n\nText", "b", "---\na: 1\nb: \n---\n\nText")]
    [InlineData("---\na: 1\n---\n\nText", "x: y", "---\na: 1\n\"x: y\": \n---\n\nText")]
    [InlineData("---\na: 1\n---\n\nText", "a", "---\na: 1\n---\n\nText")]
    public void Строка_добавления_пишет_свойство_перед_чертой(string markdown, string key, string expected)
    {
        WpfHost.Invoke(() =>
        {
            var viewer = Card(markdown);
            var window = WpfHost.Show(viewer);
            var presenter = Visuals.Descendants<MdPropertiesPresenter>(viewer).Single();

            presenter.Visibility.Should().Be(Visibility.Visible);
            MdPropertiesPresenter.AddCommand.Execute(key, presenter);
            WpfHost.Settle(window);

            viewer.Markdown.Should().Be(expected);
            window.Close();
        });
    }

    [Theory]
    [InlineData(0, "---\nb: 2\ntags:\n  - x\n---\n\nText")]
    [InlineData(1, "---\na: 1\ntags:\n  - x\n---\n\nText")]
    [InlineData(2, "---\na: 1\nb: 2\n---\n\nText")]
    public void Крестик_строки_удаляет_свойство_целиком(int row, string expected)
    {
        WpfHost.Invoke(() =>
        {
            var viewer = Card("---\na: 1\nb: 2\ntags:\n  - x\n---\n\nText");
            var window = WpfHost.Show(viewer);
            var remove = Visuals.Descendants<Button>(viewer).Where(button => button.Command == MdPropertiesPresenter.RemoveCommand).ToList();

            remove.Should().HaveCount(3);
            MdPropertiesPresenter.RemoveCommand.Execute(null, remove[row]);
            WpfHost.Settle(window);

            viewer.Markdown.Should().Be(expected);
            window.Close();
        });
    }

    [Fact]
    public void Без_правки_front_matter_флажок_выключен_а_пустая_карточка_не_видна()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = Card("---\nflag: true\n---\n\nText");
            viewer.IsFrontMatterEditable = false;
            var empty = Card("---\n\n---\n\nText");
            empty.IsFrontMatterEditable = false;
            var window = WpfHost.Show(new StackPanel { Children = { viewer, empty } });

            Visuals.Descendants<CheckBox>(viewer).Single().IsEnabled.Should().BeFalse();
            Visuals.Descendants<Button>(viewer).Where(button => button.Command == MdPropertiesPresenter.RemoveCommand).Should().OnlyContain(button => !button.IsVisible);
            Visuals.Descendants<MdPropertiesPresenter>(empty).Single().IsVisible.Should().BeFalse();
            window.Close();
        });
    }

    [Fact]
    public void Флажок_в_редакторе_отменяется_Ctrl_Z()
    {
        WpfHost.Invoke(() =>
        {
            var editor = new MarkdownEditor { Markdown = "---\nflag: true\ntags: [a]\n---\n\nText", EditMode = MdEditMode.Controls };
            editor.Extensions.Add(new PropertiesCard());
            var window = WpfHost.Show(editor);

            Visuals.Descendants<CheckBox>(editor).Single().IsChecked = false;
            WpfHost.Settle(window);
            MdPropertiesPresenter.AddItemCommand.Execute("b", Visuals.Descendants<MdPropertyValue>(editor).Last());
            WpfHost.Settle(window);
            editor.Markdown.Should().Be("---\nflag: false\ntags: [a, b]\n---\n\nText");

            ApplicationCommands.Undo.Execute(null, editor);
            ApplicationCommands.Undo.Execute(null, editor);
            WpfHost.Settle(window);
            editor.Markdown.Should().Be("---\nflag: true\ntags: [a]\n---\n\nText");
            window.Close();
        });
    }

    /// <summary>Свойство в карточке, правка контролом, свойство после неё.</summary>
    private static void Edit(string property, string expected, Action<MarkdownViewer> edit)
    {
        WpfHost.Invoke(() =>
        {
            var viewer = Card($"---\n{property}\nother: 1\n---\n\nText");
            var window = WpfHost.Show(viewer);

            edit(viewer);
            WpfHost.Settle(window);

            viewer.Markdown.Should().Be($"---\n{expected}\nother: 1\n---\n\nText");
            window.Close();
        });
    }

    private static List<PropertyVm> Rows(string markdown, params MdPropertyField[] fields)
    {
        List<PropertyVm>? rows = null;
        WpfHost.Invoke(() =>
        {
            var viewer = Card(markdown, fields);
            WpfHost.Show(viewer).Close();
            rows = [.. viewer.Items.OfType<PropertiesVm>().Single().Items];
        });

        return rows!;
    }

    private static MarkdownViewer Card(string markdown, params MdPropertyField[] fields)
    {
        var card = new PropertiesCard();
        foreach (var field in fields)
        {
            card.Fields.Add(field);
        }

        var viewer = new MarkdownViewer { Markdown = markdown, AreControlsEditable = true, IsFrontMatterEditable = true };
        viewer.Extensions.Add(card);
        return viewer;
    }
}
