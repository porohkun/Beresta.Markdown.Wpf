# Release report

This document is the right half of a diff. On the left is the same article before the edits: something
is rewritten, something added, something removed. Scroll either side — the other catches up with it by the
unchanged lines.

## What's new

- selecting text with the mouse
- highlighting of what is found
- line numbers in the margin
- line numbers in code blocks
- synchronized scrolling of two viewers

## How it works

The viewer knows the position of every source line and can scroll to a line. The sync
helper turns a line of the leading pane into a line of the following one and scrolls it there.

## Code sample

```csharp
var map = new MdLineMap(anchors);
var sync = new MdScrollSync(left, right, map);
left.ScrollToSourceLine(100, 0.38);
```

## Limitations

Scrolling has to be per pixel. Logical scrolling of lists moves by items, not
by pixels, and cannot put a line onto the anchor point that way.

## A long section

Paragraph one: text that is here for volume, so that the document does not fit into the window.

Paragraph two: text that is here for volume, so that the document does not fit into the window.

Paragraph three, rewritten: now it is longer and takes more room than on the left, so
the panes move at different speeds here, and after it they line up again.

Paragraph four: text that is here for volume, so that the document does not fit into the window.

Paragraph five: text that is here for volume, so that the document does not fit into the window.

Paragraph six: text that is here for volume, so that the document does not fit into the window.

Paragraph seven: text that is here for volume, so that the document does not fit into the window.

Paragraph eight: text that is here for volume, so that the document does not fit into the window.

## Summary

The application computes the diff, the viewer gives «what is where» and «scroll to».
