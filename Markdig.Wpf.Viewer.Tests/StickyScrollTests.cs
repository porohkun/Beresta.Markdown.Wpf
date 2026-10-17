namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Controls;
using Behaviors;

/// <summary>Прилипание прокрутки к краю: держится у края, отлипает от прокрутки и прилипает снова у края.</summary>
public sealed class StickyScrollTests
{
    [Fact]
    public void Прилипшая_к_низу_прокрутка_держится_у_конца_пока_растёт_содержимое()
    {
        WpfHost.Invoke(() =>
        {
            var (scroll, content, window) = Show(StickyEdge.Bottom);

            content.Height = 1000;
            WpfHost.Settle(window);
            scroll.VerticalOffset.Should().BeApproximately(scroll.ScrollableHeight, 1);

            content.Height = 1500;
            WpfHost.Settle(window);
            scroll.VerticalOffset.Should().BeApproximately(scroll.ScrollableHeight, 1);
            StickyScroll.GetIsStuck(scroll).Should().BeTrue();
            window.Close();
        });
    }

    [Fact]
    public void Уведённая_от_края_прокрутка_отлипает_и_прилипает_снова_у_края()
    {
        WpfHost.Invoke(() =>
        {
            var (scroll, content, window) = Show(StickyEdge.Bottom);
            content.Height = 1000;
            WpfHost.Settle(window);

            scroll.ScrollToVerticalOffset(100);
            WpfHost.Settle(window);
            content.Height = 1500;
            WpfHost.Settle(window);

            StickyScroll.GetIsStuck(scroll).Should().BeFalse();
            scroll.VerticalOffset.Should().BeApproximately(100, 1);

            scroll.ScrollToBottom();
            WpfHost.Settle(window);
            content.Height = 2000;
            WpfHost.Settle(window);

            StickyScroll.GetIsStuck(scroll).Should().BeTrue();
            scroll.VerticalOffset.Should().BeApproximately(scroll.ScrollableHeight, 1);
            window.Close();
        });
    }

    [Fact]
    public void Без_отлипания_прокрутка_возвращается_к_краю()
    {
        WpfHost.Invoke(() =>
        {
            var (scroll, content, window) = Show(StickyEdge.Bottom, canUnstick: false);
            content.Height = 1000;
            WpfHost.Settle(window);

            scroll.ScrollToVerticalOffset(100);
            WpfHost.Settle(window);
            content.Height = 1500;
            WpfHost.Settle(window);

            StickyScroll.GetIsStuck(scroll).Should().BeTrue();
            scroll.VerticalOffset.Should().BeApproximately(scroll.ScrollableHeight, 1);
            window.Close();
        });
    }

    [Fact]
    public void Прилипшая_к_верху_прокрутка_держится_у_начала_и_отлипает_так_же()
    {
        WpfHost.Invoke(() =>
        {
            var (scroll, content, window) = Show(StickyEdge.Top);
            content.Height = 1000;
            WpfHost.Settle(window);
            scroll.VerticalOffset.Should().Be(0);

            scroll.ScrollToVerticalOffset(300);
            WpfHost.Settle(window);
            content.Height = 1500;
            WpfHost.Settle(window);

            StickyScroll.GetIsStuck(scroll).Should().BeFalse();
            scroll.VerticalOffset.Should().BeApproximately(300, 1);

            scroll.ScrollToTop();
            WpfHost.Settle(window);
            StickyScroll.GetIsStuck(scroll).Should().BeTrue();
            window.Close();
        });
    }

    private static (ScrollViewer Scroll, FrameworkElement Content, Window Window) Show(StickyEdge edge, bool canUnstick = true)
    {
        var content = new Border { Height = 100 };
        var scroll = new ScrollViewer { Content = content };
        StickyScroll.SetCanUnstick(scroll, canUnstick);
        StickyScroll.SetEdge(scroll, edge);
        var window = WpfHost.Show(scroll);
        return (scroll, content, window);
    }
}
