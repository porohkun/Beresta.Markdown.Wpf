**English** | [Русский](Source-Lines-and-Two-Panes-ru)

Every element of the document knows the source lines it came from, and the viewer converts source
lines to positions on the screen and back. That is enough to keep the viewer in step with an editor,
to highlight lines, or to scroll two versions of a document side by side.

![Two versions of a document with changed lines highlighted](images/two-panes.png)

## Lines from code

Lines are counted from 0, unlike the [line numbers](Line-Numbers) on the screen. A fractional part
is a part of a line: `12.5` is the middle of line 12.

| Task | Member |
|---|---|
| where a line is | `GetSourceLineOffset(line)` — a position in the viewer's coordinates |
| what line is at a position | `GetSourceLineAt(offset)` |
| what line is visible | `GetVisibleSourceLine(fraction)` — the line at a fraction of the height of the scrolling; the `ViewportChanged` event tells when to ask again |
| scroll to a line | `ScrollToSourceLine(line, fraction)` — the line stands at a fraction of the height of the scrolling |
| a range of lines in the text map | `TextMap.RangeOfSourceLines(start, end)` — for [highlighting](Search-Highlighting) or selecting |
| the line of a text map position | `TextMap.SourceLineOf(position)` |

The scrolling around the viewer must scroll by pixels: a `ScrollViewer` does; a list needs
`VirtualizingPanel.ScrollUnit="Pixel"`.

## Following an editor

The viewer scrolls to the first visible line of a `TextBox` whenever the editor scrolls.

**`MainWindow.xaml`**:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition />
        <ColumnDefinition />
    </Grid.ColumnDefinitions>

    <TextBox x:Name="Editor"
             AcceptsReturn="True"
             FontFamily="Consolas"
             VerticalScrollBarVisibility="Auto"
             HorizontalScrollBarVisibility="Auto"
             TextChanged="OnEditorChanged"
             ScrollViewer.ScrollChanged="OnEditorScrolled" />

    <ScrollViewer Grid.Column="1">
        <mdv:MarkdownViewer x:Name="Viewer" Margin="24,16" />
    </ScrollViewer>
</Grid>
```

**`MainWindow.xaml.cs`**, in the window class:

```csharp
private void OnEditorChanged(object sender, TextChangedEventArgs e) => Viewer.Markdown = Editor.Text;

private void OnEditorScrolled(object sender, ScrollChangedEventArgs e)
{
    if (e.VerticalChange != 0)
    {
        Viewer.ScrollToSourceLine(Editor.GetFirstVisibleLineIndex());
    }
}
```

The editor does not wrap lines here: its visible lines are then the source lines.

## Two panes

`MdScrollSync` scrolls two viewers together. The pane the user works with leads, the other one
follows so that matching lines stand at the same height. Which lines match is told by `MdLineMap`:
pairs of anchor lines — for example, unchanged lines of a diff — with lines between anchors mapped
linearly. Two copies of one text need no anchors: `MdLineMap.Identity`.

**`MainWindow.xaml`**, two viewers in two scrolling panes; the window handles `Loaded` and `Unloaded`:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition />
        <ColumnDefinition />
    </Grid.ColumnDefinitions>
    <ScrollViewer>
        <mdv:MarkdownViewer x:Name="LeftViewer" Markdown="{Binding Old}" Margin="24,16" />
    </ScrollViewer>
    <ScrollViewer Grid.Column="1">
        <mdv:MarkdownViewer x:Name="RightViewer" Markdown="{Binding New}" Margin="24,16" />
    </ScrollViewer>
</Grid>
```

**`MainWindow.xaml.cs`**, in the window class (`using System.Windows.Media; using MarkdigWpfViewer.Scrolling;
using MarkdigWpfViewer.Selection;`):

```csharp
private static readonly Brush RemovedBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xE5, 0x39, 0x35));
private static readonly Brush AddedBrush = new SolidColorBrush(Color.FromArgb(0x50, 0x43, 0xA0, 0x47));

private MdScrollSync? _sync;

private void OnLoaded(object sender, RoutedEventArgs e)
{
    var left = (LeftViewer.Markdown ?? string.Empty).Split('\n');
    var right = (RightViewer.Markdown ?? string.Empty).Split('\n');
    var matches = MatchLines(left, right);

    var anchors = matches.SelectMany(match => new[]
    {
        ((double)match.Left, (double)match.Right),
        (match.Left + 1.0, match.Right + 1.0)
    });
    _sync = new MdScrollSync(LeftViewer, RightViewer, new MdLineMap(anchors));

    LeftViewer.Decorations = Highlight(LeftViewer, left.Length, matches.Select(match => match.Left), RemovedBrush);
    RightViewer.Decorations = Highlight(RightViewer, right.Length, matches.Select(match => match.Right), AddedBrush);
}

private void OnUnloaded(object sender, RoutedEventArgs e)
{
    _sync?.Dispose();
    _sync = null;
}

private static List<(int Left, int Right)> MatchLines(string[] left, string[] right)
{
    var matches = new List<(int Left, int Right)>();
    var next = 0;
    for (var i = 0; i < left.Length; i++)
    {
        var j = Array.IndexOf(right, left[i], next);
        if (j >= 0)
        {
            matches.Add((i, j));
            next = j + 1;
        }
    }

    return matches;
}

private static List<MdDecoration> Highlight(MarkdownViewer viewer, int lineCount, IEnumerable<int> matched, Brush brush)
    => Enumerable.Range(0, lineCount)
        .Except(matched)
        .Select(line => viewer.TextMap.RangeOfSourceLines(line, line))
        .OfType<MdTextRange>()
        .Select(range => new MdDecoration(range, brush))
        .ToList();
```

Each matched line is anchored at its start and at its end, so it stays aligned along its whole height.
`MatchLines` is a naive stand-in that finds equal lines in order; a real application takes the matches
from its diff.
