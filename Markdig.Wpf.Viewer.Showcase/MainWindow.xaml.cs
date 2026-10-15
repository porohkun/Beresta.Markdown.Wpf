namespace MarkdigWpfViewer.Showcase;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Diff;
using Scrolling;
using Selection;
using Window = HandyControl.Controls.Window;

/// <summary>Окно витрины: вкладка на каждый сценарий встраивания.</summary>
public partial class MainWindow : Window
{
    private static readonly Brush FoundBrush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xC1, 0x07));
    private static readonly Brush RemovedBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xE5, 0x39, 0x35));
    private static readonly Brush AddedBrush = new SolidColorBrush(Color.FromArgb(0x50, 0x43, 0xA0, 0x47));

    private MdScrollSync? _diffSync;

    public MainWindow()
    {
        InitializeComponent();
    }

    private static List<MdDecoration> Changes(MarkdownViewer viewer, IEnumerable<(int Start, int End)> chunks, Brush brush)
    {
        return chunks
            .Select(chunk => viewer.TextMap.RangeOfSourceLines(chunk.Start, chunk.End))
            .OfType<MdTextRange>()
            .Select(range => new MdDecoration(range, brush))
            .ToList();
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
            found.Add(new(new(index, query.Length), FoundBrush));
        }

        DocumentViewer.Decorations = found;
    }

    /// <summary>
    /// Дифф считает витрина: совпавшие строки — опоры синхронной прокрутки, остальные
    /// подсвечиваются как удалённые и добавленные.
    /// </summary>
    private void OnDiffLoaded(object sender, RoutedEventArgs e)
    {
        if (_diffSync is not null || DiffLeft.Markdown is not { } left || DiffRight.Markdown is not { } right)
        {
            return;
        }

        var leftLines = left.Split('\n');
        var rightLines = right.Split('\n');
        var matches = LineDiff.Match(leftLines, rightLines);

        var anchors = matches.SelectMany(match => new[]
        {
            (match.Left, match.Right),
            (match.Left + 1.0, match.Right + 1.0)
        });
        _diffSync = new(DiffLeft, DiffRight, new(anchors));

        DiffLeft.Decorations = Changes(DiffLeft, LineDiff.Unmatched(leftLines.Length, matches.Select(match => match.Left)), RemovedBrush);
        DiffRight.Decorations = Changes(DiffRight, LineDiff.Unmatched(rightLines.Length, matches.Select(match => match.Right)), AddedBrush);
    }
}