# How to show a long model answer without bringing the UI down

A language model answer arrives **in pieces**: a word, a syllable, sometimes a single letter at a time. If
the whole document is redrawn for every piece, the UI starts stuttering by the second screen of text. Below
is how a display that does not stutter works, and which part of it the viewer does by itself.

## Why you cannot redraw on every piece

A model gives out dozens of pieces a second, and the network batches them as it likes: now silence, now a
hundred at once. Every redraw of the document means parsing the text, comparing it with the previous
document and laying out the new blocks.

- There are more pieces than frames on the screen: a redraw more often than a frame is not visible, but costs.
- Parsing grows with the text: the longer the answer, the more every extra time costs.
- There is one UI thread: while it parses text, it does not scroll and does not answer the mouse.

> A good rule: show not every piece, but **everything that has piled up** when the UI is free.

## How it works

1. Pieces are appended to a common text — from any thread, a background one included.
2. The first piece after a pause goes to the viewer at once.
3. The next ones wait for the interval — a hundred milliseconds by default — and go as one batch.
4. A batch is the whole accumulated text, and the viewer applies it as edits: the blocks that did not
   change stay as they were.
5. Completion hands over the rest at once, without waiting for the interval.

However many pieces arrive, the viewer updates no more often than once per interval: for a minute of answer,
no more than six hundred parses.

```csharp
var stream = new MdMarkdownStream(Viewer) { Interval = TimeSpan.FromMilliseconds(100) };

await foreach (var piece in model.StreamAsync(question))
{
    stream.Append(piece);
}

stream.Complete();
```

## Scrolling after the answer

While the answer grows, it is handy to keep the scroll at the end. But if the person has gone to read the
beginning, pulling them down on every piece is a sure way to annoy them. So the scroll **comes unstuck**
when it is taken away from the edge, and sticks again when it is brought back to the end.

```xml
<ScrollViewer b:StickyScroll.Edge="Bottom"
              b:StickyScroll.CanUnstick="True">
    <mdv:MarkdownViewer x:Name="Viewer" Margin="24,16" />
</ScrollViewer>
```

> [!TIP]
> To keep the scroll at the end always, turn `CanUnstick` off. And if the new text arrives at the top, as in
> an event feed, set the edge to `Top`.

## Approaches compared

| Approach | Parses per answer | UI | What the application writes |
|---|---|---|---|
| The whole text on every piece | as many as pieces | stutters | a loop and a call to the UI thread |
| Its own timer and buffer | by the timer | smooth | a timer, a buffer and a call to the UI thread |
| `MdMarkdownStream` | no more often than the interval | smooth, the first piece at once | one line per piece |

## What to check in your application

- [x] pieces arrive from a background thread
- [x] the scroll does not jerk while you read the beginning
- [ ] a long code block in the middle of the answer
- [ ] a table that grows row by row

## Frequent questions

**Will the selection be lost while the text grows?** No: the selection holds on to its words, and the blocks
above the end of the answer do not change.

**What happens to an unclosed code block?** Until the fence is closed, everything after it is code. That is
as it should be: the model has not finished the block yet, and the viewer honestly shows what there is.

**And if the model sends `**bold` without the closing asterisks?** The asterisks are visible until the pair
arrives. As soon as it arrives, the word becomes bold.

**Can you start over?** Yes: `Reset` clears the viewer, and the stream accepts pieces again.

## A long continuation

What follows is just a lot of text, so that there is something to scroll while the answer is still coming.

### Step one: take a piece

A piece can come from anywhere: from a network handler, from a loop over an async enumeration, from a
timer. The viewer knows nothing about it and should not: it gets the already accumulated text, and only on
its own thread.

### Step two: accumulate

Accumulating is cheap: a piece is appended to the end of a string. Only showing is expensive, so there is
one showing per batch, not per piece.

### Step three: show

Showing parses the whole text at once. That matters: a reference link, a footnote, a list item of several
paragraphs need context, and parsing a piece apart from the rest would give a different document.

### Step four: do not shift what is visible

The new text is applied as edits. If the answer grows at the bottom while the person reads the middle,
what is visible does not move: the blocks above do not change, and their heights are already known.

### Step five: scroll if needed

A stuck scroll watches its edge. The content grew — the scroll goes back to the edge. The person moved the
scroll away — it came unstuck and no longer gets in the way. Brought it back to the edge — it sticks again.

### Step six: finish

`Complete` says that there will be no more pieces: the rest goes to the viewer, and a new piece before
`Reset` is an error, not a quiet loss of data.

### Summary

Streaming display is not a separate mode of the viewer, but an ordinary new text that comes often. All that
is needed on top is to accumulate pieces and hand them over in batches no more often than a sensible interval.
