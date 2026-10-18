namespace Beresta.Markdown.Wpf.Showcase.Showcases.Document;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Localization;
using Search;
using Selection;

/// <summary>Документ с оглавлением и поиском.</summary>
public partial class DocumentView : UserControl
{
    private readonly MdSearch _search;
    private bool _followingScroll;

    public DocumentView()
    {
        InitializeComponent();
        _search = new(Viewer);
        _search.PropertyChanged += OnSearchPropertyChanged;
        Localizer.Follow(this, UpdateCounter);
        SearchBar.DataContext = _search;
        UpdateCounter();
    }

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

    /// <summary>Enter — следующее совпадение, Shift+Enter — предыдущее.</summary>
    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            _search.Previous();
        }
        else
        {
            _search.Next();
        }

        e.Handled = true;
    }

    private void OnPrevious(object sender, RoutedEventArgs e) => _search.Previous();

    private void OnNext(object sender, RoutedEventArgs e) => _search.Next();

    private void OnSearchPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateCounter();

    private void UpdateCounter()
    {
        Counter.Text = _search.Query.Length == 0 ? string.Empty
            : _search.Count == 0 ? Localizer.Text("Document.Found.None")
            : string.Format(Localizer.Text("Document.Found.Position"), _search.CurrentIndex + 1, _search.Count);
    }
}