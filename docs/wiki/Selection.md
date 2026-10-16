**English** | [Русский](Selection-ru)

Text in the viewer is selected like in one document, not like in separate text boxes: one selection
runs across paragraphs, lists, tables and code, including the blocks that are scrolled out of view.

![A selection across blocks and highlighted search results](images/selection.png)

## Mouse and keyboard

| Action | Result |
|---|---|
| drag | selects from the press point to the pointer; past the edge of the scrolling the page scrolls along |
| Shift + click | moves the end of the selection to the click |
| double click | selects a word |
| triple click | selects a paragraph, a heading, a table cell or a code block |
| Ctrl+A | selects the whole document |
| Ctrl+C, Ctrl+Shift+C | copies — see [Copying](Copying) |

A selection does not start on interactive elements: buttons, check boxes, text boxes, lists, scroll
bars. An element of yours that handles the mouse itself gets the same treatment with
`sel:MdSelection.IsInteractive`.

**Your widget template** (see [Extensions](Extensions)):

```xml
<Canvas sel:MdSelection.IsInteractive="True" MouseLeftButtonDown="OnSketchMouseDown" />
```

## Positions: the text map

The selection is a range of positions in the **text map** — the text of the whole document as one
string, `Viewer.TextMap.Text`. It is the text you see: no Markdown markup, blocks separated by line
breaks, table cells by tabs; an image or an atomic widget is one character `MdTextMap.ObjectMark`. The
map covers the blocks that are not created too, so you search it with plain string methods. Every new
`Markdown` value builds a new map.

## Selecting from code

`Select(start, length)`, `SelectAll()` and `ClearSelection()` change the selection;
`ScrollToRange` brings it into view.

**`MainWindow.xaml.cs`**, in the window class (`using MarkdigWpfViewer.Selection;`):

```csharp
private void SelectFirst(string word)
{
    var index = Viewer.TextMap.Text.IndexOf(word, StringComparison.OrdinalIgnoreCase);
    if (index >= 0)
    {
        Viewer.Select(index, word.Length);
        Viewer.ScrollToRange(new MdTextRange(index, word.Length), 0.3);
    }
}
```

## Reading the selection

| Member | What it gives |
|---|---|
| `SelectionStart`, `SelectionLength` | the range in the text map; dependency properties, so you can bind to them |
| `SelectedText` | the selected text as the user sees it |
| `SelectedMarkdown` | the selection written as Markdown — see [Copying](Copying) |
| `SelectionChanged` | the event after the selection moves |

**`MainWindow.xaml`**, a status line next to the viewer:

```xml
<TextBlock Text="{Binding SelectionLength, ElementName=Viewer, StringFormat='Selected: {0}'}" />
```

A new text keeps the selection while its blocks exist; when they disappear, the selection is cleared.

## Turning selection off

`IsSelectionEnabled="False"` clears the selection, and neither the mouse, nor Ctrl+A, nor `Select`
selects anything until it is back.

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}" IsSelectionEnabled="False" />
```

The selection color is the theme key `Md.Selection.Brush` or the `SelectionBrush` property of a
viewer. The brush is drawn under the text, so keep it semi-transparent.

## Where the text is on the screen

`GetRangeRects(range)` returns the rectangles of a range line by line, `GetRangeBounds(range)` their
common frame; both are in the viewer's coordinates. They exist only for created blocks: a range out of
view gives an empty list and `Rect.Empty`. That is enough to put a button next to the selection.

**`MainWindow.xaml`**, the viewer and a canvas over it in one grid cell:

```xml
<ScrollViewer>
    <Grid Margin="24,16">
        <mdv:MarkdownViewer x:Name="Viewer"
                            Markdown="{Binding Text}"
                            SelectionChanged="OnSelectionMoved"
                            ViewportChanged="OnSelectionMoved" />
        <Canvas>
            <Button x:Name="AskButton" Content="Explain" Visibility="Collapsed" Focusable="False" />
        </Canvas>
    </Grid>
</ScrollViewer>
```

**`MainWindow.xaml.cs`**, in the window class (`using MarkdigWpfViewer.Selection;`):

```csharp
private void OnSelectionMoved(object? sender, EventArgs e)
{
    var bounds = Viewer.GetRangeBounds(new MdTextRange(Viewer.SelectionStart, Viewer.SelectionLength));
    AskButton.Visibility = bounds.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
    if (!bounds.IsEmpty)
    {
        Canvas.SetLeft(AskButton, bounds.Left);
        Canvas.SetTop(AskButton, bounds.Bottom + 4);
    }
}
```

The opposite direction is `GetPositionFromPoint(point, snapToText)`: the text map position under a
point. With `snapToText: false` a point outside the text gives `null`; with `true` — the nearest text.
Together with the source lines of the map it tells what line of the source is under the mouse.

**`MainWindow.xaml.cs`**, a `MouseMove` handler of the viewer; `StatusText` is a `TextBlock` of the
window:

```csharp
private void OnViewerMouseMove(object sender, MouseEventArgs e)
{
    StatusText.Text = Viewer.GetPositionFromPoint(e.GetPosition(Viewer), snapToText: false) is { } position
                      && Viewer.TextMap.SourceLineOf(position) is { } line
        ? $"Source line {line + 1}"
        : string.Empty;
}
```
