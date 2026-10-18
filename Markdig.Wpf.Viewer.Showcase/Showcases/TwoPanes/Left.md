# Release report

This document is the left half of a diff. On the right is the same article after the edits: something
is rewritten, something added, something removed. Scroll either side — the other catches up with it by the
unchanged lines.

## What's new

- selecting text with the mouse
- highlighting of what is found
- synchronized scrolling of two viewers

## How it works

The viewer knows the position of every source line and can scroll to a line. The sync
helper turns a line of the leading pane into a line of the following one and scrolls it there.

Between unchanged lines the position is recalculated linearly. That is why in a changed piece one
pane moves faster than the other.

## Code sample

```csharp
var sync = new MdScrollSync(left, right, map);
left.ScrollToSourceLine(100);
```

## Limitations

Scrolling has to be per pixel. Logical scrolling of lists moves by items, not
by pixels, and cannot put a line onto the anchor point that way.

## A long section

Paragraph one: text that is here for volume, so that the document does not fit into the window.

Paragraph two: text that is here for volume, so that the document does not fit into the window.

Paragraph three: text that is here for volume, so that the document does not fit into the window.

Paragraph four: text that is here for volume, so that the document does not fit into the window.

Paragraph five: text that is here for volume, so that the document does not fit into the window.

Paragraph six: text that is here for volume, so that the document does not fit into the window.

Paragraph seven: text that is here for volume, so that the document does not fit into the window.

Paragraph eight: text that is here for volume, so that the document does not fit into the window.

## Summary

The application computes the diff, the viewer gives «what is where» and «scroll to».
