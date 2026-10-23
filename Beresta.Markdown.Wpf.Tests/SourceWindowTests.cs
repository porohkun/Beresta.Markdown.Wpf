namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using Presenters;
using Scrolling;
using Kit;
using ViewModels;

/// <summary>Наследник ставит свой элемент вместо блоков на строках окна, и вьюер работает с ним как с блоком.</summary>
public sealed class SourceWindowTests
{
    private const double RowHeight = 30;

    private const string Text = "first\n\nsecond\nstill second\n\nthird";

    private static readonly string LongText = string.Join("\n\n", Enumerable.Range(0, 2000).Select(i => $"Paragraph {i}"));

    [Fact]
    public void Окно_заменяет_ровно_блоки_на_своих_строках()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = Text };
            var window = WpfHost.Show(viewer);
            var element = Element(2, 3);
            var map = viewer.TextMap;

            viewer.Open(2, 3, element, new EvenRows(2, 3));
            WpfHost.Settle(window);

            viewer.Items.Cast<BlockVm>().Select(Kind).Should().Equal("Paragraph", "BlankLine", "SourceWindow", "BlankLine", "Paragraph");
            Texts(viewer).Should().Equal("first", "third");
            element.IsDescendantOf(viewer).Should().BeTrue();
            viewer.TextMap.Should().BeSameAs(map);
            window.Close();
        });
    }

    [Fact]
    public void Блок_вылезший_за_край_окна_уходит_под_окно_целиком()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = Text };
            var window = WpfHost.Show(viewer);

            viewer.Open(3, 3, Element(3, 3), new EvenRows(3, 3));
            WpfHost.Settle(window);

            Texts(viewer).Should().Equal("first", "third");
            viewer.Window!.Source.StartLine.Should().Be(3);
            viewer.Spans.Select(span => (span.StartLine, span.EndLine)).Should().Contain((2, 3));
            window.Close();
        });
    }

    [Fact]
    public void Снятое_окно_возвращает_блоки()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = Text };
            var window = WpfHost.Show(viewer);
            var element = Element(2, 3);

            viewer.Open(2, 3, element, new EvenRows(2, 3));
            WpfHost.Settle(window);
            viewer.Close();
            WpfHost.Settle(window);

            Texts(viewer).Should().Equal("first", "second\nstill second", "third");
            element.IsDescendantOf(viewer).Should().BeFalse();
            window.Close();
        });
    }

    [Fact]
    public void Окно_переживает_новый_текст_и_замену_списка_целиком()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = Text };
            var window = WpfHost.Show(viewer);
            var element = Element(2, 3);
            viewer.Open(2, 3, element, new EvenRows(2, 3));
            WpfHost.Settle(window);
            var applied = viewer.Applied;

            viewer.Markdown = "first, edited\n\nsecond\nstill second\n\nthird";
            WpfHost.Settle(window);

            element.IsDescendantOf(viewer).Should().BeTrue();
            Texts(viewer).Should().Equal("first, edited", "third");

            viewer.Markdown = string.Join("\n\n", Enumerable.Range(0, 40).Select(i => $"Other {i}"));
            WpfHost.Settle(window);

            element.IsDescendantOf(viewer).Should().BeTrue();
            Visuals.Descendants<SourceWindowHost>(viewer).Should().ContainSingle();
            viewer.Applied.Should().Be(applied + 2);
            window.Close();
        });
    }

    [Fact]
    public void Окно_не_убирается_когда_его_прокрутили_далеко_за_край()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = LongText };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var element = Element(0, 0);
            viewer.Open(0, 0, element, new EvenRows(0, 0));
            WpfHost.Settle(window);

            scroll.ScrollToEnd();
            WpfHost.Settle(window);

            element.IsDescendantOf(viewer).Should().BeTrue();
            viewer.ItemContainerGenerator.ContainerFromIndex(0).Should().NotBeNull();
            viewer.ItemContainerGenerator.ContainerFromIndex(2).Should().BeNull();
            viewer.ItemContainerGenerator.ContainerFromIndex(viewer.Items.Count - 1).Should().NotBeNull();
            viewer.BlockPanel!.RealizedRange!.Value.First.Should().BeGreaterThan(2);
            window.Close();
        });
    }

    [Fact]
    public void Позиции_строк_внутри_окна_берутся_из_строк_окна()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = Text };
            var window = WpfHost.Show(viewer);
            var element = Element(2, 3);
            viewer.Open(2, 3, element, new EvenRows(2, 3));
            WpfHost.Settle(window);
            var top = element.TranslatePoint(default, viewer).Y;

            viewer.GetSourceLineOffset(2).Should().BeApproximately(top, 0.01);
            viewer.GetSourceLineOffset(3.5).Should().BeApproximately(top + 1.5 * RowHeight, 0.01);
            viewer.GetSourceLineAt(top + 1.5 * RowHeight).Should().BeApproximately(3.5, 0.01);
            window.Close();
        });
    }

    [Fact]
    public void Номера_строк_на_полях_внутри_окна_берутся_из_строк_окна()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = Text, ShowSourceLineNumbers = true };
            var window = WpfHost.Show(viewer);
            var element = Element(2, 3);
            viewer.Open(2, 3, element, new EvenRows(2, 3));
            WpfHost.Settle(window);
            var top = element.TranslatePoint(default, viewer.Gutter!).Y;

            var numbers = viewer.Gutter!.CollectNumbers();

            numbers.Should().ContainEquivalentOf((2, top, RowHeight));
            numbers.Should().ContainEquivalentOf((3, top + RowHeight, RowHeight));
            numbers.Select(number => number.Line).Should().Equal(0, 1, 2, 3, 4, 5);
            window.Close();
        });
    }

    [Fact]
    public void Удержанная_строка_окна_стоит_на_месте_когда_выше_меняются_высоты()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = LongText, Width = 300 };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var element = Element(60, 60);
            viewer.Open(60, 60, element, new EvenRows(60, 60));
            WpfHost.Settle(window);
            scroll.ScrollToVerticalOffset(element.TranslatePoint(default, viewer).Y - 250);
            WpfHost.Settle(window);
            var before = element.TranslatePoint(default, scroll).Y;

            viewer.Hold();
            viewer.Markdown = LongText.Replace("Paragraph 25\n", string.Join(" ", Enumerable.Repeat("Paragraph 25 is long now", 20)) + "\n");
            WpfHost.Settle(window);

            element.TranslatePoint(default, scroll).Y.Should().BeApproximately(before, 1);
            window.Close();
        });
    }

    [Fact]
    public void Чужая_прокрутка_отпускает_удержанную_строку()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = LongText, Width = 300 };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var element = Element(60, 60);
            viewer.Open(60, 60, element, new EvenRows(60, 60));
            WpfHost.Settle(window);
            scroll.ScrollToVerticalOffset(element.TranslatePoint(default, viewer).Y - 250);
            WpfHost.Settle(window);
            viewer.Hold();
            WpfHost.Settle(window);

            scroll.ScrollToVerticalOffset(scroll.VerticalOffset - 100);
            WpfHost.Settle(window);
            var before = element.TranslatePoint(default, scroll).Y;
            viewer.Markdown = LongText.Replace("Paragraph 25\n", string.Join(" ", Enumerable.Repeat("Paragraph 25 is long now", 20)) + "\n");
            WpfHost.Settle(window);

            element.TranslatePoint(default, scroll).Y.Should().BeGreaterThan(before + RowHeight);
            window.Close();
        });
    }

    [Fact]
    public void Пустое_место_на_строке_окна_встаёт_у_его_края()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = Text, Gaps = [new MdGap(3, 40)] };
            var window = WpfHost.Show(viewer);
            viewer.Open(2, 3, Element(2, 3), new EvenRows(2, 3));
            WpfHost.Settle(window);

            var layout = (GapLayout)viewer.GetValue(GapLayout.LayoutProperty);

            layout.Before(viewer.Window!).Should().Be(40);
            layout.Inside(viewer.Window!).Should().Be(0);
            window.Close();
        });
    }

    [Fact]
    public void Узел_автоматизации_окна_у_самого_элемента()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = Text };
            var window = WpfHost.Show(viewer);
            var element = new TextBox { Text = "second\nstill second" };
            viewer.Open(2, 3, element, new EvenRows(2, 3));
            WpfHost.Settle(window);

            var children = UIElementAutomationPeer.CreatePeerForElement(viewer).GetChildren();

            children.Should().HaveCount(5);
            children[2].Should().BeSameAs(UIElementAutomationPeer.CreatePeerForElement(element));
            window.Close();
        });
    }

    [Fact]
    public void Разметка_выделенного_берёт_и_блоки_под_окном()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = Text };
            var window = WpfHost.Show(viewer);
            viewer.Open(2, 3, Element(2, 3), new EvenRows(2, 3));
            WpfHost.Settle(window);

            viewer.SelectAll();

            viewer.SelectedMarkdown.Should().Be(Text);
            window.Close();
        });
    }

    [Fact]
    public void Окно_внутри_цитаты_заменяет_ровно_задетый_блок()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = "> first\n>\n> second\n>\n> third" };
            var window = WpfHost.Show(viewer);
            var id = viewer.Items.Cast<BlockVm>().Single().NodeId;
            var element = Element(2, 2);

            viewer.Open(2, 2, element, new EvenRows(2, 2));
            WpfHost.Settle(window);

            var quote = viewer.Items.Cast<BlockVm>().Should().ContainSingle().Which.Should().BeOfType<QuoteVm>().Subject;
            quote.NodeId.Should().Be(id);
            quote.Blocks.Select(Kind).Should().Equal("Paragraph", "BlankLine", "SourceWindow", "BlankLine", "Paragraph");
            element.IsDescendantOf(viewer).Should().BeTrue();
            Texts(viewer).Should().Equal("first", "third");
            window.Close();
        });
    }

    [Fact]
    public void Окно_на_пункте_делит_список_и_номера_второй_части_не_сбиваются()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = "1. a\n2. b\n3. c" };
            var window = WpfHost.Show(viewer);

            viewer.Open(1, 1, Element(1, 1), new EvenRows(1, 1));
            WpfHost.Settle(window);

            viewer.Items.Cast<BlockVm>().Select(Kind).Should().Equal("List", "SourceWindow", "List");
            ((ListVm)viewer.Items[2]!).Items.Select(item => item.MarkerText).Should().Equal("3.");
            Texts(viewer).Should().Equal("a", "c");
            window.Close();
        });
    }

    [Fact]
    public void Пункт_вложенного_списка_окно_в_пункте_родителе_родитель_нарисован()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = "- parent\n  - sub1\n  - sub2" };
            var window = WpfHost.Show(viewer);

            viewer.Open(1, 1, Element(1, 1), new EvenRows(1, 1));
            WpfHost.Settle(window);

            var list = viewer.Items.Cast<BlockVm>().Should().ContainSingle().Which.Should().BeOfType<ListVm>().Subject;
            list.Items[0].Blocks.Select(Kind).Should().Equal("Paragraph", "SourceWindow", "List");
            Texts(viewer).Should().Equal("parent", "sub2");
            window.Close();
        });
    }

    [Fact]
    public void Окно_на_строке_заголовка_плашки_берёт_её_целиком_а_ниже_встаёт_внутрь()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = "> [!NOTE]\n> inside\n>\n> more" };
            var window = WpfHost.Show(viewer);
            var element = Element(0, 0);

            viewer.Open(0, 0, element, new EvenRows(0, 0));
            WpfHost.Settle(window);
            viewer.Items.Cast<BlockVm>().Select(Kind).Should().Equal("SourceWindow");

            viewer.Open(1, 1, element, new EvenRows(1, 1));
            WpfHost.Settle(window);
            var callout = viewer.Items.Cast<BlockVm>().Should().ContainSingle().Which.Should().BeOfType<CalloutVm>().Subject;
            callout.Blocks.Select(Kind).Should().Equal("SourceWindow", "BlankLine", "Paragraph");
            window.Close();
        });
    }

    [Fact]
    public void Свёрнутая_плашка_правится_целиком()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = "> [!note]- Title\n> inside" };
            viewer.Extensions.Add(new ObsidianCallouts());
            var window = WpfHost.Show(viewer);

            viewer.Open(1, 1, Element(1, 1), new EvenRows(1, 1));
            WpfHost.Settle(window);

            viewer.Items.Cast<BlockVm>().Select(Kind).Should().Equal("SourceWindow");
            window.Close();
        });
    }

    [Fact]
    public void Окно_в_пункте_списка_далеко_за_краем_остаётся_созданным()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = "- parent\n  - sub\n\n" + LongText };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var element = Element(1, 1);
            viewer.Open(1, 1, element, new EvenRows(1, 1));
            WpfHost.Settle(window);

            scroll.ScrollToEnd();
            WpfHost.Settle(window);

            element.IsDescendantOf(viewer).Should().BeTrue();
            viewer.ItemContainerGenerator.ContainerFromIndex(0).Should().NotBeNull();
            viewer.ItemContainerGenerator.ContainerFromIndex(2).Should().BeNull();
            window.Close();
        });
    }

    [Fact]
    public void Удержанное_окно_внутри_цитаты_стоит_на_месте_когда_выше_меняются_высоты()
    {
        WpfHost.Invoke(() =>
        {
            var text = LongText.Replace("Paragraph 60\n", "> Paragraph 60\n>\n> quoted tail\n");
            var viewer = new WindowViewer { Markdown = text, Width = 300 };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            var element = Element(122, 122);
            viewer.Open(122, 122, element, new EvenRows(122, 122));
            WpfHost.Settle(window);
            scroll.ScrollToVerticalOffset(element.TranslatePoint(default, viewer).Y - 250);
            WpfHost.Settle(window);
            var before = element.TranslatePoint(default, scroll).Y;

            viewer.Hold();
            viewer.Markdown = text.Replace("Paragraph 25\n", string.Join(" ", Enumerable.Repeat("Paragraph 25 is long now", 20)) + "\n");
            WpfHost.Settle(window);

            viewer.Items.Cast<BlockVm>().OfType<QuoteVm>().Should().ContainSingle().Which.Blocks.Select(Kind).Should().Contain("SourceWindow");
            element.TranslatePoint(default, scroll).Y.Should().BeApproximately(before, 1);
            window.Close();
        });
    }

    [Fact]
    public void Позиции_строк_внутри_вложенного_окна_берутся_из_строк_окна()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = "> first\n>\n> second\n> still second\n>\n> third", ShowSourceLineNumbers = true };
            var window = WpfHost.Show(viewer);
            var element = Element(2, 3);
            viewer.Open(2, 3, element, new EvenRows(2, 3));
            WpfHost.Settle(window);
            var top = element.TranslatePoint(default, viewer).Y;
            var gutterTop = element.TranslatePoint(default, viewer.Gutter!).Y;

            viewer.GetSourceLineOffset(2).Should().BeApproximately(top, 0.01);
            viewer.GetSourceLineOffset(3.5).Should().BeApproximately(top + 1.5 * RowHeight, 0.01);
            viewer.GetSourceLineAt(top + 1.5 * RowHeight).Should().BeApproximately(3.5, 0.01);
            viewer.Gutter!.CollectNumbers().Should().ContainEquivalentOf((3, gutterTop + RowHeight, RowHeight));
            window.Close();
        });
    }

    [Fact]
    public void Уровни_единиц_от_верхнего_до_самого_глубокого()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new WindowViewer { Markdown = "- a\n- b\n  - c\n\n> q1\n>\n> q2" };
            var window = WpfHost.Show(viewer);

            viewer.Levels(2).Select(level => level.Units.Count).Should().Equal(3, 2, 1, 1);
            viewer.Levels(2).Select(level => level.Container.StartLine).Should().Equal(0, 0, 1, 2);
            viewer.Levels(2)[2].Indent.Should().Be(2);
            viewer.Levels(1).Should().HaveCount(2, "строка маркера принадлежит пункту целиком");
            viewer.Levels(6).Select(level => level.Container.StartLine).Should().Equal(0, 4);
            viewer.Levels(3).Should().ContainSingle();
            window.Close();
        });
    }

    private static Border Element(int first, int last)
        => new() { Height = (last - first + 1) * RowHeight, Background = Brushes.Beige };

    private static string Kind(BlockVm block) => block.GetType().Name[..^2];

    private static List<string> Texts(MarkdownViewer viewer)
        => Visuals.Descendants<MdTextBlock>(viewer)
            .Select(text => text.TryGetMapRange(out var range) ? viewer.TextMap.GetText(range) : string.Empty)
            .ToList();

    /// <summary>Строки окна одной высоты, все видны.</summary>
    private sealed class EvenRows(int first, int last) : IMdSourceRows
    {
        public event EventHandler? RowsChanged
        {
            add { }
            remove { }
        }

        public double? GetSourceLineOffset(double line) => (Math.Clamp(line, first, last + 1) - first) * RowHeight;

        public double? GetSourceLineAt(double offset) => first + Math.Clamp(offset / RowHeight, 0, last + 1 - first);

        public IReadOnlyList<(int Line, double Top, double Height)> VisibleRows()
            => Enumerable.Range(first, last - first + 1)
                .Select(line => (line, (line - first) * RowHeight, RowHeight))
                .ToList();
    }

    /// <summary>Наследник вьюера, который открывает защищённое API окна тестам.</summary>
    private sealed class WindowViewer : MarkdownViewer
    {
        public int Applied { get; private set; }

        public IReadOnlyList<MdSourceSpan> Spans => TopBlockSpans;

        public SourceWindowVm? Window => Items.OfType<SourceWindowVm>().SingleOrDefault();

        public void Open(int first, int last, FrameworkElement element, IMdSourceRows rows) => SetSourceWindow(first, last, element, rows);

        public void Close() => ClearSourceWindow();

        public void Hold() => HoldSourceWindow();

        public IReadOnlyList<MdSourceLevel> Levels(int line) => SourceLevelsAt(line);

        protected override void OnDocumentApplied() => Applied++;
    }
}
