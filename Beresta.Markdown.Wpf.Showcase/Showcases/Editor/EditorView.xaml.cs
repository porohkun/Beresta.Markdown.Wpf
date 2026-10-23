namespace Beresta.Markdown.Wpf.Showcase.Showcases.Editor;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Beresta.Markdown.Wpf.Editor;
using Localization;

/// <summary>Редактор во всю ширину витрины, с поиском и заменой в исходнике.</summary>
public partial class EditorView : UserControl
{
    private readonly MdSourceSearch _search;

    public EditorView()
    {
        InitializeComponent();
        _search = new(Editor);
        _search.PropertyChanged += (_, _) => UpdateCounter();
        Localizer.Follow(this, UpdateCounter);
        SearchBar.DataContext = _search;
        UpdateCounter();
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

    private void OnReplace(object sender, RoutedEventArgs e) => _search.Replace(ReplaceBox.Text);

    private void OnReplaceAll(object sender, RoutedEventArgs e) => _search.ReplaceAll(ReplaceBox.Text);

    private void UpdateCounter()
    {
        Counter.Text = _search.Query.Length == 0 ? string.Empty
            : _search.Count == 0 ? Localizer.Text("Document.Found.None")
            : string.Format(Localizer.Text("Document.Found.Position"), _search.CurrentIndex + 1, _search.Count);
    }
}
