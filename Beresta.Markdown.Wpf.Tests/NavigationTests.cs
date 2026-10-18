namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using Selection;

/// <summary>Позиции карты: под точкой, в строках исходника, прокрутка к куску.</summary>
public sealed class NavigationTests
{
    private const string ThreeParagraphs = "first paragraph\n\nsecond **bold** paragraph\n\nthird paragraph";

    [Fact]
    public void Позиция_под_точкой_знака_совпадает_с_самим_знаком()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);

            foreach (var offset in new[] { 0, 3, viewer.TextMap.Text.IndexOf("bold", StringComparison.Ordinal), viewer.TextMap.Length - 1 })
            {
                viewer.GetPositionFromPoint(Visuals.PointOf(viewer, offset), snapToText: false).Should().Be(offset);
            }

            window.Close();
        });
    }

    [Fact]
    public void Вне_текста_позиция_есть_только_с_привязкой_к_тексту()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            var below = new Point(10, viewer.ActualHeight - 5);

            viewer.GetPositionFromPoint(below, snapToText: false).Should().BeNull();
            viewer.GetPositionFromPoint(below, snapToText: true).Should().Be(viewer.TextMap.Length);
            window.Close();
        });
    }

    [Fact]
    public void Позиция_карты_знает_свою_строку_исходника()
    {
        WpfHost.Invoke(() =>
        {
            const string markdown = "first\nsecond line\n\n```\ncode1\ncode2\n```\n\n| a | b |\n|---|---|\n| c | d |";
            var viewer = new MarkdownViewer { Markdown = markdown };
            var window = WpfHost.Show(viewer);
            var map = viewer.TextMap;

            map.SourceLineOf(0).Should().Be(0);
            map.SourceLineOf(map.Text.IndexOf("second", StringComparison.Ordinal)).Should().Be(1);
            map.SourceLineOf(map.Text.IndexOf("code2", StringComparison.Ordinal)).Should().Be(5);
            map.SourceLineOf(map.Text.LastIndexOf('d')).Should().Be(10);
            window.Close();
        });
    }

    [Fact]
    public void Кусок_в_несозданном_блоке_прокручивается_в_долю_высоты()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n\n", Enumerable.Range(0, 2000).Select(i => $"Paragraph {i}"));
            var viewer = new MarkdownViewer { Markdown = text };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var target = new MdTextRange(viewer.TextMap.Text.IndexOf("Paragraph 1500", StringComparison.Ordinal), 14);

            viewer.ScrollToRange(target, 0.25).Should().BeTrue();
            WpfHost.Settle(window);

            var head = viewer.GetRangeRects(target).Should().ContainSingle().Subject;
            viewer.TranslatePoint(head.TopLeft, scroll).Y.Should().BeApproximately(0.25 * scroll.ViewportHeight, 1);
            window.Close();
        });
    }

    [Fact]
    public void Кусок_внутри_длинного_абзаца_встаёт_точно()
    {
        WpfHost.Invoke(() =>
        {
            var words = string.Join(" ", Enumerable.Range(0, 400).Select(i => $"word{i}"));
            var text = string.Join("\n\n", Enumerable.Range(0, 50).Select(i => $"Intro {i}"))
                       + "\n\n" + words + "\n\n"
                       + string.Join("\n\n", Enumerable.Range(0, 50).Select(i => $"Outro {i}"));
            var viewer = new MarkdownViewer { Markdown = text };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var target = new MdTextRange(viewer.TextMap.Text.IndexOf("word350", StringComparison.Ordinal), 7);

            viewer.ScrollToRange(target).Should().BeTrue();
            WpfHost.Settle(window);

            var head = viewer.GetRangeRects(target).Should().ContainSingle().Subject;
            viewer.TranslatePoint(head.TopLeft, scroll).Y.Should().BeApproximately(0, 1);
            window.Close();
        });
    }
}