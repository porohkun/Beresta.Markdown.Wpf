namespace Beresta.Markdown.Wpf.Showcase.Showcases.Chat;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Localization;

/// <summary>Лента сообщений в одной прокрутке; сверху — сколько вьюеров создано.</summary>
public partial class ChatView : UserControl
{
    private int _created;
    private Panel? _panel;

    public ChatView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Localizer.Follow(this, UpdateCounter);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        _panel = Visuals.FirstDescendant<VirtualizingStackPanel>(Messages);
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
            CultureInfo.CurrentUICulture,
            Localizer.Text("Chat.Counter"),
            count,
            _created,
            _panel?.Children.Count ?? 0);
    }
}
