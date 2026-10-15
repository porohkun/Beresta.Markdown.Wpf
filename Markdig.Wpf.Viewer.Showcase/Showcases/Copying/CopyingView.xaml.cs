namespace MarkdigWpfViewer.Showcase.Showcases.Copying;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

/// <summary>
/// Документ слева, содержимое буфера обмена справа. Ctrl+C во вьюере обрабатывает вкладка, после
/// копирования буфер перечитывается.
/// </summary>
public partial class CopyingView : UserControl
{
    private const double ButtonGap = 4;

    public CopyingView()
    {
        InitializeComponent();
        AddHandler(CommandManager.ExecutedEvent, new ExecutedRoutedEventHandler(OnExecuted), true);
    }

    private CopyingViewModel Model => (CopyingViewModel)DataContext;

    private static void SetText(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (COMException)
        {
            // BYDESIGN: буфер обмена занят другой программой — копирование просто не удалось.
        }
    }

    private void OnCanCopy(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = Viewer.SelectionLength > 0;

    private void OnCopy(object sender, ExecutedRoutedEventArgs e)
    {
        switch (Model.Choice.Kind)
        {
            case CopyKind.Viewer:
                Viewer.Copy();
                break;
            case CopyKind.Text:
                SetText(Viewer.SelectedText);
                break;
            case CopyKind.Markdown:
                SetText(Viewer.SelectedMarkdown);
                break;
        }
    }

    private async void OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Command == ApplicationCommands.Copy)
        {
            await Model.RefreshAsync();
        }
    }

    private async void OnRefresh(object sender, RoutedEventArgs e) => await Model.RefreshAsync();

    /// <summary>Кнопка встаёт над серединой выделения, а у верхнего края — под ним.</summary>
    private void OnSelectionMoved(object? sender, EventArgs e)
    {
        var bounds = Viewer.GetRangeBounds(new(Viewer.SelectionStart, Viewer.SelectionLength));
        if (bounds.IsEmpty)
        {
            SelectionButton.Visibility = Visibility.Collapsed;
            return;
        }

        SelectionButton.Visibility = Visibility.Visible;
        SelectionButton.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
        var size = SelectionButton.DesiredSize;
        var left = bounds.Left + (bounds.Width - size.Width) / 2;
        var top = bounds.Top - size.Height - ButtonGap;

        Canvas.SetLeft(SelectionButton, Math.Clamp(left, 0, Math.Max(0, Viewer.ActualWidth - size.Width)));
        Canvas.SetTop(SelectionButton, top >= 0 ? top : bounds.Bottom + ButtonGap);
    }
}
