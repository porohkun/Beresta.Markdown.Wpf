namespace MarkdigWpfViewer.Showcase.Showcases.TwoPanes;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Localization;
using Scrolling;
using Selection;

/// <summary>
/// Две панели: дифф считает витрина. Синхронную прокрутку ведёт <c>MdScrollSync</c>, а правку одной
/// фигурой — подсветка строк слева, соединение, подсветка справа и общий контур — витрина рисует сама
/// по координатам вьюеров; панели вровень ставит <c>MdPaneAligner</c>. Строки правок — пометками во всю
/// ширину и полосами на полях, правки внутри строк — второй подсветкой, чертой и рамками.
/// </summary>
public partial class TwoPanesView : UserControl
{
    private MdScrollSync? _sync;
    private MdPaneAligner? _aligner;
    private List<(int LeftStart, int LeftEnd, int RightStart, int RightEnd)> _hunks = [];

    public TwoPanesView()
    {
        InitializeComponent();
        Localizer.Follow(this, Rebuild);
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

    /// <summary>Полоса куска строк в координатах соединений; пустой кусок — граница перед его строкой.</summary>
    private static (double Top, double Bottom)? Band(MdCoordinates coordinates, int start, int end)
    {
        if (end >= start)
        {
            return coordinates.LinesRect(start, end) is { } rect ? (rect.Top, rect.Bottom) : null;
        }

        return coordinates.LineTop(start) is { } top ? (top, top) : null;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Connect();

    /// <summary>Дифф текстов панелей: пометки правок, связь прокрутки и выравнивание.</summary>
    private void Connect()
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

        _hunks = LineDiff.Hunks(matches, leftLines, rightLines).ToList();
        _sync = new(SyncLeft, SyncRight, map);
        _aligner = new(AlignedLeft, AlignedRight, map);

        var removed = LineDiff.Unmatched(leftLines, matches.Select(match => match.Left)).ToList();
        var added = LineDiff.Unmatched(rightLines, matches.Select(match => match.Right)).ToList();
        MarkChanges(SyncLeft, removed, "Removed");
        MarkChanges(SyncRight, added, "Added");
        MarkChanges(AlignedLeft, removed, "Removed");
        MarkChanges(AlignedRight, added, "Added");

        var (leftPieces, rightPieces) = MarkPieces();
        SyncLeft.MarkLayers.Add(new MdMarkLayer("Pieces") { Marks = leftPieces });
        AlignedLeft.MarkLayers.Add(new MdMarkLayer("Pieces") { Marks = leftPieces });
        SyncRight.MarkLayers.Add(new MdMarkLayer("Pieces") { Marks = rightPieces });
        AlignedRight.MarkLayers.Add(new MdMarkLayer("Pieces") { Marks = rightPieces });

        SyncLeft.ViewportChanged += OnPanesMoved;
        SyncRight.ViewportChanged += OnPanesMoved;
    }

    /// <summary>
    /// Правки внутри строк изменённых кусков: дописанное подсвечено, а на его месте с другой стороны —
    /// черта; удалённое — наоборот; заменённое — удалённое слева и дописанное справа.
    /// </summary>
    private (List<MdMark> Left, List<MdMark> Right) MarkPieces()
    {
        var (left, right) = (new List<MdMark>(), new List<MdMark>());
        foreach (var hunk in _hunks.Where(hunk => hunk.LeftEnd >= hunk.LeftStart && hunk.RightEnd >= hunk.RightStart))
        {
            if (SyncLeft.TextMap.RangeOfSourceLines(hunk.LeftStart, hunk.LeftEnd) is not { } from
                || SyncRight.TextMap.RangeOfSourceLines(hunk.RightStart, hunk.RightEnd) is not { } to)
            {
                continue;
            }

            foreach (var piece in TextDiff.Pieces(SyncLeft.TextMap.GetText(from), SyncRight.TextMap.GetText(to)))
            {
                var leftPiece = new MdTextRange(from.Start + piece.LeftStart, piece.LeftLength);
                var rightPiece = new MdTextRange(to.Start + piece.RightStart, piece.RightLength);
                if (piece.LeftLength == 0)
                {
                    left.Add(new MdMark(leftPiece, MdMarkKind.Caret) { Style = "Added" });
                    right.Add(new MdMark(rightPiece) { Style = "Added" });
                }
                else if (piece.RightLength == 0)
                {
                    left.Add(new MdMark(leftPiece) { Style = "Removed" });
                    right.Add(new MdMark(rightPiece, MdMarkKind.Caret) { Style = "Removed" });
                }
                else
                {
                    left.Add(new MdMark(leftPiece) { Style = "Removed" });
                    right.Add(new MdMark(rightPiece) { Style = "Added" });
                }
            }
        }

        return (left, right);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => Disconnect();

    private void Disconnect()
    {
        SyncLeft.ViewportChanged -= OnPanesMoved;
        SyncRight.ViewportChanged -= OnPanesMoved;
        _sync?.Dispose();
        _aligner?.Dispose();
        _sync = null;
        _aligner = null;
    }

    /// <summary>Тексты сменились вместе с языком: дифф, пометки и связь панелей — заново.</summary>
    private void Rebuild()
    {
        Disconnect();
        foreach (var viewer in new[] { SyncLeft, SyncRight, AlignedLeft, AlignedRight })
        {
            viewer.MarkLayers.Clear();
        }

        Connect();
        DrawConnections();
    }

    private void OnPanesMoved(object? sender, EventArgs e) => DrawConnections();

    private void OnConnectionsSizeChanged(object sender, SizeChangedEventArgs e) => DrawConnections();

    /// <summary>Левый и правый края вьюера в координатах соединений.</summary>
    private (double Left, double Right) Edges(MarkdownViewer viewer)
    {
        var left = viewer.TranslatePoint(default, Connections).X;
        return (left, left + viewer.ActualWidth);
    }

    /// <summary>Кисть от цвета левого оформления у левого края соединения к цвету правого у правого.</summary>
    private LinearGradientBrush Gradient(string leftStyle, string rightStyle, string part, double from, double to)
        => new(ColorOf($"Md.Mark.{leftStyle}.{part}"), ColorOf($"Md.Mark.{rightStyle}.{part}"), new Point(from, 0), new Point(to, 0))
        {
            MappingMode = BrushMappingMode.Absolute
        };

    /// <summary>Цвет кисти темы вместе с её прозрачностью.</summary>
    private Color ColorOf(string key)
        => FindResource(key) is SolidColorBrush { Color: var color } brush
            ? Color.FromArgb((byte)Math.Round(color.A * brush.Opacity), color.R, color.G, color.B)
            : Colors.Transparent;

    /// <summary>
    /// Каждая правка — одна фигура из подсветки строк, соединения между панелями и контура: вставка —
    /// цветом добавленного, удаление — цветом удалённого, изменение — переходом от одного к другому.
    /// </summary>
    private void DrawConnections()
    {
        Connections.Children.Clear();
        var left = SyncLeft.GetCoordinates(Connections);
        var right = SyncRight.GetCoordinates(Connections);
        var (leftStart, leftEnd) = Edges(SyncLeft);
        var (rightStart, rightEnd) = Edges(SyncRight);
        var gapStart = Gap.Offset;
        var gapEnd = gapStart + Gap.ActualWidth;

        foreach (var hunk in _hunks)
        {
            if (Band(left, hunk.LeftStart, hunk.LeftEnd) is not { } from
                || Band(right, hunk.RightStart, hunk.RightEnd) is not { } to
                || Math.Max(from.Bottom, to.Bottom) < 0
                || Math.Min(from.Top, to.Top) > Connections.ActualHeight)
            {
                continue;
            }

            var leftStyle = hunk.LeftEnd < hunk.LeftStart ? "Added" : "Removed";
            var rightStyle = hunk.RightEnd < hunk.RightStart ? "Removed" : "Added";
            var fill = Gradient(leftStyle, rightStyle, "Brush", gapStart, gapEnd);
            var stroke = Gradient(leftStyle, rightStyle, "LineBrush", gapStart, gapEnd);

            Connections.Children.Add(new Polygon
            {
                Points =
                {
                    new(leftEnd, from.Top), new(gapStart, from.Top), new(gapEnd, to.Top), new(rightStart, to.Top),
                    new(rightStart, to.Bottom), new(gapEnd, to.Bottom), new(gapStart, from.Bottom), new(leftEnd, from.Bottom)
                },
                Fill = fill
            });
            Connections.Children.Add(new Polygon
            {
                Points =
                {
                    new(leftStart, from.Top), new(gapStart, from.Top), new(gapEnd, to.Top), new(rightEnd, to.Top),
                    new(rightEnd, to.Bottom), new(gapEnd, to.Bottom), new(gapStart, from.Bottom), new(leftStart, from.Bottom)
                },
                Stroke = stroke,
                StrokeThickness = 1
            });
        }
    }
}
