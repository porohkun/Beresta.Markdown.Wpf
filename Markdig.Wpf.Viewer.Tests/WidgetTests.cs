namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Extensions;
using Kit;
using Markdig.Syntax;
using Presenters;
using ViewModels;

/// <summary>Виджеты: состояние вне контрола и высота, которая меняется после загрузки.</summary>
public sealed class WidgetTests
{
    private static readonly string Paragraphs = string.Join("\n\n", Enumerable.Range(0, 300).Select(i => $"Paragraph {i}"));

    [Fact]
    public void Состояние_блочного_виджета_переживает_виртуализацию_и_новый_текст()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Extensions = { Toggle() }, Markdown = "```toggle\n```\n\n" + Paragraphs };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            Visuals.Descendants<CheckBox>(viewer).Single().IsChecked = true;

            scroll.ScrollToEnd();
            WpfHost.Settle(window);
            Visuals.Descendants<CheckBox>(viewer).Should().BeEmpty("виджет ушёл из видимого вместе с контролом");

            viewer.Markdown = "Intro\n\n```toggle\n```\n\n" + Paragraphs;
            scroll.ScrollToHome();
            WpfHost.Settle(window);

            Visuals.Descendants<CheckBox>(viewer).Single().IsChecked.Should().BeTrue();
            window.Close();
        });
    }

    [Fact]
    public void Состояние_строчного_виджета_переживает_новый_текст()
    {
        WpfHost.Invoke(() =>
        {
            var mention = TestWidgets.Mention();
            var template = StateCheckBox();
            var stateful = new LambdaExtension
            {
                Setup = mention.Setup,
                Inline = mention.Inline,
                Template = item => item is TestWidgets.MentionVm ? template : null,
                State = node => node is TestWidgets.MentionVm ? new ToggleState() : null
            };
            var viewer = new MarkdownViewer { Extensions = { stateful }, Markdown = "hello @ivan and @maria" };
            var window = WpfHost.Show(viewer);
            Visuals.Descendants<CheckBox>(viewer).First().IsChecked = true;

            viewer.Markdown = "Intro\n\nhello @ivan and @maria";
            WpfHost.Settle(window);

            Visuals.Descendants<CheckBox>(viewer).Select(box => box.IsChecked).Should().Equal(true, false);
            window.Close();
        });
    }

    [Fact]
    public void Состояние_вложенного_виджета_не_достаётся_внешнему_и_наоборот()
    {
        WpfHost.Invoke(() =>
        {
            var note = TestWidgets.Note();
            var outer = new LambdaExtension
                { Block = note.Block, Template = note.Template, State = node => node is TestWidgets.NoteVm ? new ToggleState() : null };
            var viewer = new MarkdownViewer { Extensions = { outer, Toggle() }, Markdown = ":::note T\ninside\n\n```toggle\n```\n:::" };
            var window = WpfHost.Show(viewer);

            var noteBorder = Visuals.Descendants<Border>(viewer).Single(border => Equals(border.Tag, "note"));
            var box = Visuals.Descendants<CheckBox>(viewer).Single();
            MdWidget.GetState(noteBorder).Should().BeOfType<ToggleState>();
            MdWidget.GetState(box).Should().BeOfType<ToggleState>().And.NotBeSameAs(MdWidget.GetState(noteBorder));
            MdWidget.GetState(Visuals.Descendants<MdTextBlock>(viewer).Single()).Should().BeNull();
            window.Close();
        });
    }

    [Fact]
    public void Выросший_после_загрузки_виджет_выше_видимого_не_сдвигает_видимое()
    {
        WpfHost.Invoke(() =>
        {
            var template = TestWidgets.Template("<Border Tag='late' Height='20' />");
            var late = new LambdaExtension
            {
                Block = (block, _) => block is FencedCodeBlock { Info: "late" } ? new LateVm() : null,
                Template = item => item is LateVm ? template : null
            };
            var before = string.Join("\n\n", Enumerable.Range(0, 40).Select(i => $"Before {i}"));
            var viewer = new MarkdownViewer { Extensions = { late }, Markdown = before + "\n\n```late\n```\n\n" + Paragraphs };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });
            var firstAfter = 40 * 2 + 3;

            viewer.ScrollToSourceLine(firstAfter + 2 * 4).Should().BeTrue();
            WpfHost.Settle(window);
            var visible = viewer.GetVisibleSourceLine()!.Value;
            var border = Visuals.Descendants<Border>(viewer).Single(element => Equals(element.Tag, "late"));

            border.Height = 220;
            WpfHost.Settle(window);

            viewer.GetVisibleSourceLine().Should().BeApproximately(visible, 0.05);
            window.Close();
        });
    }

    /// <summary>Галочка, привязанная к состоянию своего виджета.</summary>
    [Fact]
    public void Новый_документ_не_получает_состояний_и_выделения_прежнего()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Extensions = { Toggle() }, DocumentKey = "a", Markdown = "one two three\n\n```toggle\n```" };
            var window = WpfHost.Show(viewer);
            Visuals.Descendants<CheckBox>(viewer).Single().IsChecked = true;
            viewer.Select(4, 3);

            viewer.DocumentKey = "b";
            viewer.Markdown = "uno dos tres\n\n```toggle\n```";
            WpfHost.Settle(window);

            Visuals.Descendants<CheckBox>(viewer).Single().IsChecked.Should().BeFalse();
            viewer.SelectionLength.Should().Be(0);
            window.Close();
        });
    }

    [Fact]
    public void Ключ_документа_после_текста_тоже_начинает_новый_документ()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Extensions = { Toggle() }, DocumentKey = "a", Markdown = "one two three\n\n```toggle\n```" };
            var window = WpfHost.Show(viewer);
            Visuals.Descendants<CheckBox>(viewer).Single().IsChecked = true;
            viewer.Select(4, 3);

            viewer.Markdown = "uno dos tres\n\n```toggle\n```";
            viewer.DocumentKey = "b";
            WpfHost.Settle(window);

            Visuals.Descendants<CheckBox>(viewer).Single().IsChecked.Should().BeFalse();
            viewer.SelectionLength.Should().Be(0);
            window.Close();
        });
    }

    [Fact]
    public void Refresh_заново_спрашивает_резолвер_и_сохраняет_выделение_и_состояния()
    {
        WpfHost.Invoke(() =>
        {
            var pages = new Pages();
            var viewer = new MarkdownViewer
            {
                Extensions = { Toggle(), new WikiLinks { Resolver = pages } },
                Markdown = "See [[Later]] here.\n\n```toggle\n```"
            };
            var window = WpfHost.Show(viewer);
            var box = Visuals.Descendants<CheckBox>(viewer).Single();
            box.IsChecked = true;
            viewer.Select(0, 3);
            StyleNames(viewer).Should().Contain("WikiLink.Missing");

            pages.Known.Add("Later");
            viewer.Refresh();
            WpfHost.Settle(window);

            StyleNames(viewer).Should().Contain("WikiLink").And.NotContain("WikiLink.Missing");
            Visuals.Descendants<CheckBox>(viewer).Single().Should().BeSameAs(box);
            box.IsChecked.Should().BeTrue();
            viewer.SelectedText.Should().Be("See");
            window.Close();
        });
    }

    private static List<string?> StyleNames(MarkdownViewer viewer)
        => viewer.Items.OfType<ParagraphVm>()
            .SelectMany(paragraph => paragraph.Inlines.OfType<RunVm>())
            .Select(run => run.StyleName)
            .ToList();

    private static DataTemplate StateCheckBox()
    {
        var box = new FrameworkElementFactory(typeof(CheckBox));
        box.SetBinding(ToggleButton.IsCheckedProperty,
            new Binding
            {
                Path = new("(0).IsOn", MdWidget.StateProperty),
                RelativeSource = RelativeSource.Self
            });

        return new() { VisualTree = box };
    }

    private static LambdaExtension Toggle()
    {
        var template = StateCheckBox();
        return new()
        {
            Block = (block, _) => block is FencedCodeBlock { Info: "toggle" } ? new ToggleVm() : null,
            Template = item => item is ToggleVm ? template : null,
            State = node => node is ToggleVm ? new ToggleState() : null
        };
    }

    public sealed class ToggleState
    {
        public bool IsOn { get; set; }
    }

    private sealed class Pages : IMdLinkResolver
    {
        public HashSet<string> Known { get; } = [];

        public bool PageExists(string page) => Known.Contains(page);
    }

    private sealed record ToggleVm : BlockVm;

    private sealed record LateVm : BlockVm;
}