namespace MarkdigWpfViewer.Showcase;

using System.Windows.Controls;
using System.Windows.Media;
using Selection;

/// <summary>Окно витрины: вкладка на каждый сценарий встраивания.</summary>
public partial class MainWindow : HandyControl.Controls.Window
{
    private static readonly Brush FoundBrush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xC1, 0x07));

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Найденное в текстовой карте подсвечивается декорациями вьюера.</summary>
    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text;
        var text = DocumentViewer.TextMap.Text;
        var found = new List<MdDecoration>();

        for (var index = string.IsNullOrEmpty(query) ? -1 : text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
             index >= 0;
             index = text.IndexOf(query, index + query.Length, StringComparison.OrdinalIgnoreCase))
        {
            found.Add(new MdDecoration(new MdTextRange(index, query.Length), FoundBrush));
        }

        DocumentViewer.Decorations = found;
    }
}
