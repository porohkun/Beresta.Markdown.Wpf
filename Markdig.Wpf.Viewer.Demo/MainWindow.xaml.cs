namespace MarkdigWpfViewer.Demo;

using System.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Ссылки своей схемы перехватываются здесь; всё остальное остаётся необработанным и уходит
    /// наружу системным обработчиком.
    /// </summary>
    private void OnMarkdownLink(object sender, MarkdownLinkEventArgs e)
    {
        if (!e.Href.StartsWith("demo:", StringComparison.Ordinal))
            return;

        MessageBox.Show(this, e.Href, "Своя ссылка");
        e.Handled = true;
    }
}
