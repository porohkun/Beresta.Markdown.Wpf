namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Threading;

/// <summary>
/// Один поток WPF на все тесты: приложение с темой вьюера, окна за пределами экрана.
/// </summary>
internal static class WpfHost
{
    private static readonly Lazy<Dispatcher> Host = new(Start);

    public static void Invoke(Action action) => Host.Value.Invoke(action);

    /// <summary>Показывает содержимое в окне за пределами экрана и доводит раскладку до конца.</summary>
    public static Window Show(UIElement content, double height = 400)
    {
        var window = new Window
        {
            Width = 600,
            Height = height,
            Left = -10000,
            Top = -10000,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Content = content
        };

        window.Show();
        Settle(window);
        return window;
    }

    /// <summary>Прогоняет раскладку и очередь диспетчера, пока отклики на прокрутку не улягутся.</summary>
    public static void Settle(Window window)
    {
        for (var i = 0; i < 5; i++)
        {
            window.UpdateLayout();

            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
            Dispatcher.PushFrame(frame);
        }
    }

    private static Dispatcher Start()
    {
        Dispatcher? dispatcher = null;
        using var ready = new ManualResetEventSlim();

        var thread = new Thread(() =>
        {
            var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            application.Resources.MergedDictionaries.Add(new()
            {
                Source = new("pack://application:,,,/Beresta.Markdown.Wpf;component/Themes/Generic.xaml")
            });

            dispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        })
        {
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
        return dispatcher!;
    }
}