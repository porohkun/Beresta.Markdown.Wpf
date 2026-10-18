# Bookmarks in the margin

To the left of the text is a column of the application, not of the viewer. The circles in it are ordinary WPF
buttons. Click a circle — the line becomes a bookmark: the application puts a full-width mark on it.

Only visible lines have buttons: scroll the document — the column rebuilds. A long line of a
paragraph wraps onto several screen lines, and the button still stands at its start — the viewer knows
where each source line begins.

## How it is done

The column and the viewer are in one scroll. The viewer tells which lines are visible and where each of
them stands in the coordinates of the column, and reports when the layout changes. The rest is up to the application:

- it creates buttons for the visible lines with text;
- puts each one in the middle of its screen line;
- places the buttons again after every change of the layout;
- shows a bookmark as a mark of its own layer.

```csharp
foreach (var (line, top, height) in Viewer.GetCoordinates(Marks).VisibleRows())
{
    // the button goes to top in the column, the line height is height
}
```

## Lines of different heights

A heading, a paragraph, a list item, a table row and a line of code each have a height of their own, and the
button lands exactly on its line.

| What | Line height |
|---|---|
| heading | a large font |
| paragraph | ordinary text |
| code | a monospaced font |

> A quote is made of source lines too, and each of them has a button. The second line of the quote.
> The third line of the quote.

## Long text

The paragraphs below are here so that there is something to scroll. The first paragraph. The viewer creates
only the visible blocks, and the column only the visible buttons, so the length of the document does not matter to them.

The second paragraph. This source line is very long: in the window it takes several screen lines, and the button stands at the first of them, because the viewer knows where each source line begins even when the text wraps.
This is the second line of the same paragraph: it starts on a new screen line, and it has a button of its own.

The third paragraph. Bookmarks are marks of the full-width row kind in the application's layer. They show
over the text and over the background of the blocks.

The fourth paragraph. Scroll to the end and back: the buttons follow the lines.

The fifth paragraph. A bit more text so that the document is longer than the window.

The sixth paragraph. And a bit more.

The seventh paragraph. The end of the document is near.

The eighth paragraph. The last one.
