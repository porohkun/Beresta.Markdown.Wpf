namespace MarkdigWpfViewer.Showcase.Showcases.TwoPanes;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Scrolling;
using Selection;

/// <summary>Две панели: дифф считает витрина, вьюеры синхронизирует <c>MdScrollSync</c>.</summary>
public partial class TwoPanesView : UserControl
{
    private static readonly Brush RemovedBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xE5, 0x39, 0x35));
    private static readonly Brush AddedBrush = new SolidColorBrush(Color.FromArgb(0x50, 0x43, 0xA0, 0x47));

    private MdScrollSync? _sync;

    public TwoPanesView()
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

    /// <summary>
    /// Совпавшие строки — опоры синхронной прокрутки, остальные подсвечиваются как удалённые и
    /// добавленные.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_sync is not null || LeftViewer.Markdown is not { } left || RightViewer.Markdown is not { } right)
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
        _sync = new(LeftViewer, RightViewer, new(anchors));

        LeftViewer.Decorations = Changes(LeftViewer, LineDiff.Unmatched(leftLines.Length, matches.Select(match => match.Left)), RemovedBrush);
        RightViewer.Decorations = Changes(RightViewer, LineDiff.Unmatched(rightLines.Length, matches.Select(match => match.Right)), AddedBrush);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _sync?.Dispose();
        _sync = null;
    }
}