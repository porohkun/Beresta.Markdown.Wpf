namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Presenters;
using Selection;
using ViewModels;

/// <summary>Выделение сквозь блоки: мышь, подсветка, копирование, неделимые элементы.</summary>
public sealed class SelectionTests
{
    private const string ThreeParagraphs = "first paragraph\n\nsecond **bold** paragraph\n\nthird paragraph";

    [Fact]
    public void Попадание_мышью_возвращает_смещение_каждого_знака()
    {
        WpfHost.Invoke(() =>
        {
            var block = new MdTextBlock
            {
                Width = 120,
                TextWrapping = TextWrapping.Wrap,
                Runs = [new RunVm("Hello "), new RunVm("bold words", Bold: true), new RunVm(" and a link", Hyperlink: new("https://e.com"))]
            };
            var window = WpfHost.Show(block);

            for (var offset = 0; offset < 27; offset++)
            {
                var rect = block.GetRangeRects(offset, offset + 1).First();
                block.HitTestLocal(new(rect.Left + 0.5, rect.Top + rect.Height / 2)).Should().Be(offset, $"знак {offset}");
            }

            window.Close();
        });
    }

    [Fact]
    public void Подсветка_переносится_по_строкам()
    {
        WpfHost.Invoke(() =>
        {
            var block = new MdTextBlock
            {
                Width = 80,
                TextWrapping = TextWrapping.Wrap,
                Runs = [new RunVm("one two three four five six seven")]
            };
            var window = WpfHost.Show(block);

            var rects = block.GetRangeRects(0, 33).ToList();
            rects.Should().HaveCountGreaterThan(1);
            rects.Select(rect => rect.Top).Should().BeInAscendingOrder();
            window.Close();
        });
    }

    [Fact]
    public void Перетаскивание_выделяет_куски_нескольких_блоков()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            var text = viewer.TextMap.Text;

            var from = PointOf(viewer, text.IndexOf("paragraph", StringComparison.Ordinal));
            var to = PointOf(viewer, text.IndexOf("third", StringComparison.Ordinal) + 3);

            viewer.Selection.Press(from, extend: false).Should().BeTrue();
            viewer.Selection.Drag(to).Should().BeTrue();
            viewer.Selection.Release().Should().BeTrue();

            viewer.SelectedText.Should().Be("paragraph\n\nsecond bold paragraph\n\nthi");
            window.Close();
        });
    }

    [Fact]
    public void Щелчок_без_перетаскивания_не_выделяет()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);

            viewer.Selection.Press(PointOf(viewer, 3), extend: false);
            viewer.Selection.Release().Should().BeFalse();

            viewer.SelectionLength.Should().Be(0);
            window.Close();
        });
    }

    [Fact]
    public void Shift_продлевает_выделение_от_якоря()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);

            viewer.Selection.Press(PointOf(viewer, 0), extend: false);
            viewer.Selection.Release();
            viewer.Selection.Press(PointOf(viewer, 5), extend: true);
            viewer.Selection.Release();

            viewer.SelectedText.Should().Be("first");
            window.Close();
        });
    }

    [Fact]
    public void Подсвечены_только_блоки_внутри_выделения()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            var third = viewer.TextMap.Text.IndexOf("third", StringComparison.Ordinal);

            viewer.Select(0, third - 2);
            WpfHost.Settle(window);

            var blocks = Visuals.Descendants<MdTextBlock>(viewer).ToList();
            blocks[0].Highlights.Should().ContainSingle();
            blocks[1].Highlights.Should().ContainSingle();
            blocks[2].Highlights.Should().BeEmpty();
            AdornerLayer.GetAdornerLayer(blocks[0])!.GetAdorners(blocks[0]).Should().ContainSingle();
            window.Close();
        });
    }

    [Fact]
    public void Декорации_подсвечивают_свои_куски_под_выделением()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);

            viewer.Decorations = [new(new(0, 5), Brushes.Yellow)];
            viewer.Select(2, 10);
            WpfHost.Settle(window);

            var first = Visuals.Descendants<MdTextBlock>(viewer).First();
            first.Highlights.Select(highlight => highlight.Brush).Should().Equal(Brushes.Yellow, viewer.SelectionBrush!);
            window.Close();
        });
    }

    [Fact]
    public void Неделимые_блок_и_кусок_строки_подсвечиваются_целиком()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "a `code` b\n\n---\n\nc" };
            var window = WpfHost.Show(viewer);

            viewer.SelectAll();
            WpfHost.Settle(window);

            Visuals.Descendants<MdBlockItem>(viewer).Should().Contain(item => item.Content is ThematicBreakVm && item.Highlight != null);
            var code = Visuals.Descendants<InlineCodePresenter>(viewer).Single();
            var overlay = ((Grid)VisualTreeHelper.GetParent(code)).Children.OfType<Border>().Single();
            overlay.Background.Should().Be(viewer.SelectionBrush);
            viewer.SelectedText.Should().Be("a code b\n\n\n\nc");
            window.Close();
        });
    }

    [Fact]
    public void Копирование_берёт_и_несозданные_блоки()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n\n", Enumerable.Range(0, 2000).Select(i => $"Paragraph {i}"));
            var viewer = new MarkdownViewer { Markdown = text };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });

            viewer.SelectAll();

            viewer.SelectedText.Should().Be(text);
            window.Close();
        });
    }

    [Fact]
    public void Перетаскивание_за_край_прокручивает_и_продолжает_выделение()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n\n", Enumerable.Range(0, 2000).Select(i => $"Paragraph {i}"));
            var viewer = new MarkdownViewer { Markdown = text };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);

            viewer.Selection.Press(PointOf(viewer, 0), extend: false);
            var below = scroll.TranslatePoint(new(20, scroll.ActualHeight + 40), viewer);
            viewer.Selection.Drag(below);
            var before = viewer.SelectionLength;

            for (var i = 0; i < 10; i++)
            {
                viewer.Selection.AutoScrollStep();
            }

            scroll.VerticalOffset.Should().BeGreaterThan(0);
            viewer.SelectionLength.Should().BeGreaterThan(before);
            viewer.Selection.Release();
            window.Close();
        });
    }

    [Fact]
    public void Нажатие_на_кнопку_внутри_виджета_выделение_не_начинает()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer();
            var button = new Button();
            var text = new TextBlock();

            SelectionController.IsInteractive(button, viewer).Should().BeTrue();
            SelectionController.IsInteractive(text, viewer).Should().BeFalse();
        });
    }

    [Fact]
    public void Команды_выделить_всё_и_копировать_доступны_во_вьюере()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);

            ApplicationCommands.Copy.CanExecute(null, viewer).Should().BeFalse();
            ApplicationCommands.SelectAll.Execute(null, viewer);

            viewer.SelectionLength.Should().Be(viewer.TextMap.Length);
            ApplicationCommands.Copy.CanExecute(null, viewer).Should().BeTrue();
            window.Close();
        });
    }

    [Fact]
    public void Новый_текст_сбрасывает_выделение()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            viewer.SelectAll();

            viewer.Markdown = "other";

            viewer.SelectionLength.Should().Be(0);
            window.Close();
        });
    }

    [Fact]
    public void Блок_далеко_внизу_прокручивается_в_видимую_область()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n\n", Enumerable.Range(0, 2000).Select(i => $"Paragraph {i}"));
            var viewer = new MarkdownViewer { Markdown = text };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var panel = Visuals.Descendants<MdBlockPanel>(viewer).First();

            panel.BringIndexIntoViewPublic(3000);
            WpfHost.Settle(window);

            var container = (FrameworkElement)viewer.ItemContainerGenerator.ContainerFromIndex(3000);
            container.Should().NotBeNull();
            var top = container.TransformToAncestor(scroll).Transform(new(0, 0)).Y;
            top.Should().BeInRange(0, scroll.ViewportHeight);
            window.Close();
        });
    }

    private static Point PointOf(MarkdownViewer viewer, int offset)
    {
        foreach (var text in Visuals.Descendants<MdTextBlock>(viewer))
        {
            if (text.TryGetMapRange(out var range) && offset >= range.Start && offset < range.End)
            {
                var rect = text.GetRangeRects(offset - range.Start, offset - range.Start + 1).First();
                return text.TranslatePoint(new(rect.Left + 0.5, rect.Top + rect.Height / 2), viewer);
            }
        }

        throw new InvalidOperationException($"Знак {offset} не найден среди созданных блоков.");
    }
}