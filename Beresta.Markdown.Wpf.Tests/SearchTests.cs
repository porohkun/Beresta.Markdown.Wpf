namespace Beresta.Markdown.Wpf.Tests;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Presenters;
using Search;
using Selection;

/// <summary>Поиск: совпадения, текущее, переход с прокруткой, новый текст, поиск в ленте.</summary>
public sealed class SearchTests
{
    [Theory]
    [InlineData("Cat cat CAT", "cat", false, false, new[] { 0, 4, 8 })]
    [InlineData("Cat cat CAT", "cat", true, false, new[] { 4 })]
    [InlineData("cat concat cat_x cat", "cat", false, true, new[] { 0, 17 })]
    [InlineData("aaaa", "aa", false, false, new[] { 0, 2 })]
    [InlineData("text", "", false, false, new int[0])]
    public void Совпадения_ищутся_без_наложения_с_регистром_и_словом_целиком(
        string text,
        string query,
        bool matchCase,
        bool wholeWord,
        int[] starts)
    {
        TextMatches.Find(text, query, matchCase, wholeWord).Select(range => range.Start).Should().Equal(starts);
    }

    [Fact]
    public void Поиск_подсвечивает_совпадения_и_текущее_своими_кистями()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "one word, two words\n\nthird word" };
            var window = WpfHost.Show(viewer);
            using var search = new MdSearch(viewer) { Brush = Brushes.Yellow, CurrentBrush = Brushes.Orange };

            search.Query = "word";
            WpfHost.Settle(window);

            search.Count.Should().Be(3);
            search.CurrentIndex.Should().Be(0);
            var first = Visuals.Descendants<MdTextBlock>(viewer).First();
            first.Highlights.Select(highlight => highlight.Brush).Should().Equal(Brushes.Yellow, Brushes.Orange, Brushes.Yellow);
            window.Close();
        });
    }

    [Fact]
    public void Дальше_и_назад_ходят_по_кругу_и_сообщают_о_смене()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "a word\n\nb word\n\nc word" };
            var window = WpfHost.Show(viewer);
            using var search = new MdSearch(viewer) { Query = "word" };
            var changed = new List<string?>();
            search.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            search.Next().Should().BeTrue();
            search.Next().Should().BeTrue();
            search.CurrentIndex.Should().Be(2);
            search.Next();
            search.CurrentIndex.Should().Be(0);
            search.Previous();
            search.CurrentIndex.Should().Be(2);

            changed.Should().Contain(nameof(MdSearch.CurrentIndex));
            viewer.TextMap.GetText(search.Current!.Value).Should().Be("word");
            window.Close();
        });
    }

    [Fact]
    public void Без_совпадений_переход_не_удаётся()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "text" };
            var window = WpfHost.Show(viewer);
            using var search = new MdSearch(viewer) { Query = "absent" };

            search.Next().Should().BeFalse();
            search.CurrentIndex.Should().Be(-1);
            search.Current.Should().BeNull();
            window.Close();
        });
    }

    [Fact]
    public void Переход_прокручивает_к_совпадению_далеко_внизу()
    {
        WpfHost.Invoke(() =>
        {
            var text = string.Join("\n\n", Enumerable.Range(0, 300).Select(i => i == 250 ? "the needle is here" : $"Paragraph {i}"));
            var viewer = new MarkdownViewer { Markdown = text };
            var scroll = new ScrollViewer { Content = viewer };
            var window = WpfHost.Show(scroll);
            using var search = new MdSearch(viewer);

            search.Query = "needle";
            WpfHost.Settle(window);

            var rects = viewer.GetRangeRects(search.Current!.Value);
            rects.Should().NotBeEmpty();
            var top = viewer.TranslatePoint(rects[0].TopLeft, scroll).Y;
            top.Should().BeInRange(0, scroll.ViewportHeight);
            window.Close();
        });
    }

    [Fact]
    public void Новый_текст_ищется_заново_и_текущее_остаётся_у_прежнего_места()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "first word\n\nsecond word\n\nthird word" };
            var window = WpfHost.Show(viewer);
            using var search = new MdSearch(viewer) { Query = "word" };
            search.Next();
            search.CurrentIndex.Should().Be(1);

            viewer.Markdown = "a word above\n\nfirst word\n\nsecond word\n\nthird word";

            search.Count.Should().Be(4);
            search.CurrentIndex.Should().Be(2);
            var current = search.Current!.Value;
            viewer.TextMap.Text[..current.Start].Should().EndWith("second ");
            window.Close();
        });
    }

    [Fact]
    public void Закрытый_поиск_убирает_свой_слой()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "word" };
            var search = new MdSearch(viewer) { Query = "word" };
            viewer.MarkLayers.Should().ContainSingle();

            search.Dispose();

            viewer.MarkLayers.Should().BeEmpty();
        });
    }

    [Fact]
    public void Поиск_в_ленте_подсвечивает_каждый_вьюер_и_снимается()
    {
        WpfHost.Invoke(() =>
        {
            var feed = new StackPanel();
            feed.Children.Add(new MarkdownViewer { Markdown = "hello world" });
            feed.Children.Add(new MarkdownViewer { Markdown = "other world, world" });
            var window = WpfHost.Show(feed);

            MdFeedSearch.SetQuery(feed, "world");
            feed.Children.Add(new MarkdownViewer { Markdown = "late world" });
            WpfHost.Settle(window);

            var found = (Brush)feed.FindResource("Md.Mark.Found.Brush");
            Visuals.Descendants<MdTextBlock>(feed)
                .Select(text => text.Highlights.Count(highlight => highlight.Brush == found))
                .Should().Equal(1, 2, 1);

            MdFeedSearch.SetQuery(feed, null);
            WpfHost.Settle(window);

            Visuals.Descendants<MdTextBlock>(feed).Should().OnlyContain(text => !text.Highlights.Any());
            window.Close();
        });
    }
}
