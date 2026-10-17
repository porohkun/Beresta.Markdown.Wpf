namespace MarkdigWpfViewer.Tests;

using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Presenters;
using Selection;
using ViewModels;

/// <summary>Выделение сквозь блоки: мышь, подсветка, копирование, координаты кусков, неделимые элементы.</summary>
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
    public void Выключенное_выделение_снимает_прежнее_и_не_начинается_заново()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            viewer.SelectAll();
            viewer.SelectionLength.Should().BePositive();

            viewer.IsSelectionEnabled = false;
            viewer.SelectionLength.Should().Be(0);

            viewer.Selection.Press(PointOf(viewer, 3), extend: false).Should().BeFalse();
            viewer.SelectAll();
            viewer.Select(0, 5);
            viewer.SelectionLength.Should().Be(0);
            ApplicationCommands.Copy.CanExecute(null, viewer).Should().BeFalse();

            viewer.IsSelectionEnabled = true;
            viewer.Select(0, 5);
            viewer.SelectedText.Should().Be("first");
            window.Close();
        });
    }

    [Fact]
    public void Копируются_разом_текст_и_разметка()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            viewer.SelectAll();

            var data = viewer.CreateCopyData().Should().BeOfType<DataObject>().Subject;

            data.GetText().Should().Be("first paragraph\n\nsecond bold paragraph\n\nthird paragraph");
            var markdown = data.GetData(MdDataFormats.Markdown).Should().BeOfType<MemoryStream>().Subject;
            Encoding.UTF8.GetString(markdown.ToArray()).Should().Be(ThreeParagraphs);
            window.Close();
        });
    }

    [Fact]
    public void Пустое_выделение_не_копируется()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);

            viewer.CreateCopyData().Should().BeNull();
            window.Close();
        });
    }

    [Fact]
    public void Приложение_дописывает_свой_формат_в_Copying()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var host = new Border { Child = viewer };
            DataObject.AddCopyingHandler(host, (_, e) => e.DataObject.SetData(DataFormats.Html, "<p>html</p>"));
            var window = WpfHost.Show(host);
            viewer.SelectAll();

            var data = viewer.CreateCopyData().Should().BeOfType<DataObject>().Subject;

            data.GetData(DataFormats.Html).Should().Be("<p>html</p>");
            data.GetText().Should().Be(viewer.SelectedText);
            window.Close();
        });
    }

    [Fact]
    public void Отменённое_в_Copying_копирование_ничего_не_даёт()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            DataObject.AddCopyingHandler(viewer, (_, e) => e.CancelCommand());
            var window = WpfHost.Show(viewer);
            viewer.SelectAll();

            viewer.CreateCopyData().Should().BeNull();
            window.Close();
        });
    }

    [Fact]
    public void Команду_копирования_берёт_вьюер_а_выключенное_копирование_отдаёт_приложению()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var host = new Border { Child = viewer };
            var asked = 0;
            var copied = 0;
            host.CommandBindings.Add(new(
                ApplicationCommands.Copy,
                (_, _) => copied++,
                (_, e) =>
                {
                    asked++;
                    e.CanExecute = true;
                }));
            var window = WpfHost.Show(host);
            viewer.SelectAll();

            ApplicationCommands.Copy.CanExecute(null, viewer).Should().BeTrue();
            asked.Should().Be(0);

            viewer.IsCopyEnabled = false;
            ApplicationCommands.Copy.Execute(null, viewer);

            copied.Should().Be(1);
            viewer.CreateCopyData().Should().NotBeNull();
            window.Close();
        });
    }

    [Fact]
    public void Кнопка_копирования_следит_за_выделением_и_флагом()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var button = new Button { Command = ApplicationCommands.Copy, CommandTarget = viewer };
            var window = WpfHost.Show(new StackPanel { Children = { button, viewer } });
            button.IsEnabled.Should().BeFalse();

            viewer.Select(0, 5);
            WpfHost.Settle(window);
            button.IsEnabled.Should().BeTrue();

            viewer.IsCopyEnabled = false;
            WpfHost.Settle(window);
            button.IsEnabled.Should().BeFalse();
            window.Close();
        });
    }

    [Fact]
    public void Прямоугольник_куска_строки_лежит_на_его_знаках()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            var bold = viewer.TextMap.Text.IndexOf("bold", StringComparison.Ordinal);

            var rect = viewer.GetRangeRects(new(bold, 4)).Should().ContainSingle().Subject;

            Enumerable.Range(bold, 4).Should().OnlyContain(offset => rect.Contains(PointOf(viewer, offset)));
            rect.Contains(PointOf(viewer, bold - 2)).Should().BeFalse();
            rect.Contains(PointOf(viewer, bold + 5)).Should().BeFalse();
            window.Close();
        });
    }

    [Fact]
    public void Кусок_через_несколько_блоков_даёт_строки_каждого_и_общую_рамку()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            var text = viewer.TextMap.Text;
            var range = MdTextRange.Between(
                text.IndexOf("paragraph", StringComparison.Ordinal),
                text.IndexOf("third", StringComparison.Ordinal) + 5);

            var rects = viewer.GetRangeRects(range);
            var bounds = viewer.GetRangeBounds(range);

            rects.Select(rect => Math.Round(rect.Top)).Distinct().Should().HaveCount(3);
            rects.Should().OnlyContain(rect => bounds.Contains(rect));
            bounds.Top.Should().Be(rects.Min(rect => rect.Top));
            bounds.Bottom.Should().Be(rects.Max(rect => rect.Bottom));
            window.Close();
        });
    }

    [Fact]
    public void Неделимые_блок_и_кусок_строки_дают_рамку_целиком()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "a `code` b\n\n---\n\nc" };
            var window = WpfHost.Show(viewer);

            var rects = viewer.GetRangeRects(new(0, viewer.TextMap.Length));

            var rule = Visuals.Descendants<MdBlockItem>(viewer).Single(item => item.Content is ThematicBreakVm);
            var code = Visuals.Descendants<InlineCodePresenter>(viewer).Single();
            var overlay = ((Grid)VisualTreeHelper.GetParent(code)).Children.OfType<Border>().Single();
            rects.Should().Contain(rect => IsNear(rect, BoundsIn(viewer, rule)));
            rects.Should().Contain(rect => IsNear(rect, BoundsIn(viewer, overlay)));
            window.Close();
        });
    }

    [Fact]
    public void Кусок_на_несозданных_блоках_прямоугольников_не_имеет()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n\n", Enumerable.Range(0, 2000).Select(i => $"Paragraph {i}"));
            var viewer = new MarkdownViewer { Markdown = text };
            var window = WpfHost.Show(new ScrollViewer { Content = viewer });
            var last = new MdTextRange(viewer.TextMap.Text.LastIndexOf("Paragraph", StringComparison.Ordinal), 9);

            viewer.GetRangeRects(last).Should().BeEmpty();
            viewer.GetRangeBounds(last).IsEmpty.Should().BeTrue();
            viewer.GetRangeRects(new(0, 9)).Should().ContainSingle();
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
    public void Событие_выделения_приходит_только_при_его_смене()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var changes = 0;
            viewer.SelectionChanged += (_, _) => changes++;
            var window = WpfHost.Show(viewer);

            viewer.Markdown = "other text";
            viewer.ClearSelection();
            changes.Should().Be(0);

            viewer.Select(0, 5);
            viewer.Select(0, 5);
            viewer.ClearSelection();
            changes.Should().Be(2);
            window.Close();
        });
    }

    [Fact]
    public void Выделение_в_исчезнувшем_блоке_снимается()
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
    public void Выделение_переживает_дописанный_текст()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            viewer.Select(viewer.TextMap.Text.IndexOf("second", StringComparison.Ordinal), 6);

            viewer.Markdown = ThreeParagraphs + " and more\n\nfourth paragraph";

            viewer.SelectedText.Should().Be("second");
            window.Close();
        });
    }

    [Fact]
    public void Выделение_переезжает_вместе_со_своим_блоком()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ThreeParagraphs };
            var window = WpfHost.Show(viewer);
            var third = viewer.TextMap.Text.IndexOf("third", StringComparison.Ordinal);
            viewer.Select(third, 5);

            viewer.Markdown = "inserted above\n\n" + ThreeParagraphs;

            viewer.SelectedText.Should().Be("third");
            viewer.SelectionStart.Should().BeGreaterThan(third);
            window.Close();
        });
    }

    [Fact]
    public void Выделение_держится_за_свои_слова_при_правке_абзаца_перед_ними()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "Hello world again" };
            var window = WpfHost.Show(viewer);
            viewer.Select(viewer.TextMap.Text.IndexOf("world", StringComparison.Ordinal), 5);

            viewer.Markdown = "Oh, hello big world again";

            viewer.SelectedText.Should().Be("world");
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

    private static Rect BoundsIn(Visual root, FrameworkElement element)
        => element.TransformToAncestor(root).TransformBounds(new(element.RenderSize));

    private static bool IsNear(Rect actual, Rect expected)
        => Math.Abs(actual.X - expected.X) < 0.01
           && Math.Abs(actual.Y - expected.Y) < 0.01
           && Math.Abs(actual.Width - expected.Width) < 0.01
           && Math.Abs(actual.Height - expected.Height) < 0.01;

    private static Point PointOf(MarkdownViewer viewer, int offset) => Visuals.PointOf(viewer, offset);
}