namespace MarkdigWpfViewer.Showcase.Showcases.TwoPanes;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Scrolling;
using Selection;

/// <summary>
/// Две панели: дифф считает витрина. Синхронную прокрутку ведёт <c>MdScrollSync</c>, а соединения
/// правок между панелями витрина рисует сама по координатам вьюеров; панели вровень ставит
/// <c>MdPaneAligner</c>. Правки — пометками строк во всю ширину и полосами на полях.
/// </summary>
public partial class TwoPanesView : UserControl
{
    private MdScrollSync? _sync;
    private MdPaneAligner? _aligner;
    private List<(int LeftStart, int LeftEnd, int RightStart, int RightEnd)> _hunks = [];

    public TwoPanesView()
    {
        InitializeComponent();
    }

    private static void MarkChanges(MarkdownViewer viewer, IEnumerable<(int Start, int End)> chunks, string style)
    {
        viewer.MarkLayers.Add(new MdMarkLayer("Diff")
        {
            Marks = chunks
                .SelectMany(chunk => new[]
                {
                    new MdMark(chunk.Start, chunk.End) { Style = style },
                    new MdMark(chunk.Start, chunk.End, MdMarkKind.Bar) { Style = style }
                })
                .ToList()
        });
    }

    /// <summary>Полоса куска строк в координатах соединений; пустой кусок — точка перед его строкой.</summary>
    private static (double Top, double Bottom)? Band(MdCoordinates coordinates, int start, int end)
    {
        if (end >= start)
        {
            return coordinates.LinesRect(start, end) is { } rect ? (rect.Top, rect.Bottom) : null;
        }

        return coordinates.LineTop(start) is { } top ? (top, top) : null;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_sync is not null || DataContext is not TwoPanesViewModel model)
        {
            return;
        }

        var leftLines = model.Left.Replace("\r\n", "\n").Split('\n');
        var rightLines = model.Right.Replace("\r\n", "\n").Split('\n');
        var matches = LineDiff.Match(leftLines, rightLines);
        var map = new MdLineMap(matches.SelectMany(match => new[]
        {
            ((double)match.Left, (double)match.Right),
            (match.Left + 1.0, match.Right + 1.0)
        }));

        _hunks = LineDiff.Hunks(matches, leftLines.Length, rightLines.Length).ToList();
        _sync = new(SyncLeft, SyncRight, map);
        _aligner = new(AlignedLeft, AlignedRight, map);

        var removed = LineDiff.Unmatched(leftLines.Length, matches.Select(match => match.Left)).ToList();
        var added = LineDiff.Unmatched(rightLines.Length, matches.Select(match => match.Right)).ToList();
        MarkChanges(SyncLeft, removed, "Removed");
        MarkChanges(SyncRight, added, "Added");
        MarkChanges(AlignedLeft, removed, "Removed");
        MarkChanges(AlignedRight, added, "Added");

        SyncLeft.ViewportChanged += OnPanesMoved;
        SyncRight.ViewportChanged += OnPanesMoved;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SyncLeft.ViewportChanged -= OnPanesMoved;
        SyncRight.ViewportChanged -= OnPanesMoved;
        _sync?.Dispose();
        _aligner?.Dispose();
        _sync = null;
        _aligner = null;
    }

    private void OnPanesMoved(object? sender, EventArgs e) => DrawConnections();

    private void OnConnectionsSizeChanged(object sender, SizeChangedEventArgs e) => DrawConnections();

    /// <summary>Каждая правка — четырёхугольник от её строк слева к её строкам справа.</summary>
    private void DrawConnections()
    {
        Connections.Children.Clear();
        var left = SyncLeft.GetCoordinates(Connections);
        var right = SyncRight.GetCoordinates(Connections);
        var width = Connections.ActualWidth;

        foreach (var hunk in _hunks)
        {
            if (Band(left, hunk.LeftStart, hunk.LeftEnd) is not { } from
                || Band(right, hunk.RightStart, hunk.RightEnd) is not { } to
                || Math.Max(from.Bottom, to.Bottom) < 0
                || Math.Min(from.Top, to.Top) > Connections.ActualHeight)
            {
                continue;
            }

            var style = hunk.LeftEnd < hunk.LeftStart ? "Added" : hunk.RightEnd < hunk.RightStart ? "Removed" : "Changed";
            Connections.Children.Add(new Polygon
            {
                Points = { new(0, from.Top), new(width, to.Top), new(width, to.Bottom), new(0, from.Bottom) },
                Fill = (Brush)FindResource($"Md.Mark.{style}.Brush"),
                Stroke = (Brush)FindResource($"Md.Mark.{style}.LineBrush"),
                StrokeThickness = 1
            });
        }
    }
}
