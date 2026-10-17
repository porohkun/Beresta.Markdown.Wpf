namespace MarkdigWpfViewer.Showcase.Showcases.Chat;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

/// <summary>Лента сообщений в одной прокрутке; сверху — сколько вьюеров создано.</summary>
public partial class ChatView : UserControl
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    private int _created;
    private Panel? _panel;

    public ChatView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        _panel = FindPanel(Messages);
        var scroll = (ScrollViewer)Messages.Template.FindName("Scroll", Messages);
        scroll.ScrollChanged += (_, _) => UpdateCounter();
    }

    private void OnViewerInitialized(object? sender, EventArgs e)
    {
        _created++;
        UpdateCounter();
    }

    private void UpdateCounter()
    {
        var count = (DataContext as ChatViewModel)?.Messages.Count ?? 0;
        Counter.Text = string.Format(
            Russian,
            "Сообщений: {0:N0}. Вьюеров создано: {1:N0}, в ленте сейчас: {2:N0}.",
            count,
            _created,
            _panel?.Children.Count ?? 0);
    }

    private static Panel? FindPanel(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if ((child as VirtualizingStackPanel ?? FindPanel(child)) is { } panel)
            {
                return panel;
            }
        }

        return null;
    }
}
