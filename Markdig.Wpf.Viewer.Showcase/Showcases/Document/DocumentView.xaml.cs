namespace MarkdigWpfViewer.Showcase.Showcases.Document;

using System.Windows.Controls;
using System.Windows.Media;
using Selection;

/// <summary>Документ с оглавлением и подсветкой найденного.</summary>
public partial class DocumentView : UserControl
{
    private static readonly Brush FoundBrush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xC1, 0x07));

    private bool _followingScroll;

    public DocumentView()
    {
        InitializeComponent();
    }

    private void OnTaskToggled(object? sender, MdTaskToggledEventArgs e)
        => ((DocumentViewModel)DataContext).Toggle(e.Task, e.IsChecked);

    private void OnContentsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_followingScroll && Contents.SelectedItem is MdHeading heading)
        {
            Viewer.ScrollToRange(heading.Range);
        }
    }

    /// <summary>Текущий — последний заголовок выше верхнего края видимого.</summary>
    private void OnViewportChanged(object? sender, EventArgs e)
    {
        if (Viewer.GetVisibleSourceLine() is not { } line)
        {
            return;
        }

        _followingScroll = true;
        Contents.SelectedItem = Viewer.TextMap.Headings.LastOrDefault(heading => heading.SourceLine <= line + 0.5);
        _followingScroll = false;
    }

    /// <summary>Найденное в текстовой карте подсвечивается декорациями вьюера.</summary>
    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text;
        var text = Viewer.TextMap.Text;
        var found = new List<MdDecoration>();

        for (var index = string.IsNullOrEmpty(query) ? -1 : text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
             index >= 0;
             index = text.IndexOf(query, index + query.Length, StringComparison.OrdinalIgnoreCase))
        {
            found.Add(new(new(index, query.Length), FoundBrush));
        }

        Viewer.Decorations = found;
    }
}
