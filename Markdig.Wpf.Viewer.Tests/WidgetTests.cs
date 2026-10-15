namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Extensions;
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
            var viewer = new MarkdownViewer { Extensions = [Toggle()], Markdown = "```toggle\n```\n\n" + Paragraphs };
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
            var viewer = new MarkdownViewer { Extensions = [stateful], Markdown = "hello @ivan and @maria" };
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
            var outer = new LambdaExtension { Block = note.Block, Template = note.Template, State = node => node is TestWidgets.NoteVm ? new ToggleState() : null };
            var viewer = new MarkdownViewer { Extensions = [outer, Toggle()], Markdown = ":::note T\ninside\n\n```toggle\n```\n:::" };
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
            var viewer = new MarkdownViewer { Extensions = [late], Markdown = before + "\n\n```late\n```\n\n" + Paragraphs };
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
    private static DataTemplate StateCheckBox()
    {
        var box = new FrameworkElementFactory(typeof(CheckBox));
        box.SetBinding(ToggleButton.IsCheckedProperty, new Binding
        {
            Path = new PropertyPath("(0).IsOn", MdWidget.StateProperty),
            RelativeSource = RelativeSource.Self
        });

        return new DataTemplate { VisualTree = box };
    }

    private static LambdaExtension Toggle()
    {
        var template = StateCheckBox();
        return new LambdaExtension
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

    private sealed record ToggleVm : BlockVm;

    private sealed record LateVm : BlockVm;
}
