namespace MarkdigWpfViewer.Showcase.Showcases.MarginButtons;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Selection;

/// <summary>
/// Колонка приложения в той же прокрутке, что и вьюер: кнопки у видимых строк с текстом,
/// закладки — пометками своего слоя.
/// </summary>
public partial class MarginButtonsView : UserControl
{
    private readonly MdMarkLayer _bookmarks = new("Bookmarks");
    private readonly HashSet<int> _marked = [];

    public MarginButtonsView()
    {
        InitializeComponent();
        Viewer.MarkLayers.Add(_bookmarks);
    }

    private void OnViewportChanged(object? sender, EventArgs e) => PlaceButtons();

    /// <summary>Ставит кнопки заново: у видимых строк с текстом, по середине каждой строки в координатах колонки.</summary>
    private void PlaceButtons()
    {
        Marks.Children.Clear();
        var lines = (Viewer.Markdown ?? string.Empty).Split('\n');

        foreach (var (line, top, height) in Viewer.GetCoordinates(Marks).VisibleRows())
        {
            if (line >= lines.Length || string.IsNullOrWhiteSpace(lines[line]))
            {
                continue;
            }

            var button = new ToggleButton
            {
                Style = (Style)FindResource("LineButton"),
                IsChecked = _marked.Contains(line),
                Tag = line,
                ToolTip = $"Строка {line + 1}"
            };
            button.Click += OnToggle;
            Canvas.SetLeft(button, 8);
            Canvas.SetTop(button, top + (height - button.Height) / 2);
            Marks.Children.Add(button);
        }
    }

    private void OnToggle(object sender, RoutedEventArgs e)
    {
        var line = (int)((ToggleButton)sender).Tag;
        if (!_marked.Remove(line))
        {
            _marked.Add(line);
        }

        _bookmarks.Marks = _marked
            .Select(marked => new MdMark(marked, marked) { Style = "Comment" })
            .ToList();
    }
}
