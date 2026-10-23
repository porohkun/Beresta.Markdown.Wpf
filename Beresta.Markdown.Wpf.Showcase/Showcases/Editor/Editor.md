---
title: Editing in place
tags: [markdown, editor]
draft: false
date: 2026-10-22
---

# Editing in place

The source of this document lives in AvalonEdit. The blocks around the place being edited stay
drawn, and the edited ones show their source with markup.

## Tasks

- [x] Draw the document
- [ ] Toggle a task with a click
- [/] A task status from the ready-made extension

## Collapsible

<details>
<summary>Click to expand</summary>

The state of a collapsible block is written back into the source.

</details>

> [!note]- A folded callout
> It remembers whether it is open, too.

## Links and edits

A [[Wiki page]] and a [[Wiki page#Section|caption]], a #tag and @anna.

> [!WARNING]
> Ready-made extensions look in the source as they do in the drawing.

Edits: {++inserted++}, {--deleted--}, {~~old~>new~~}, {==marked==} {>>a comment<<}.

## Text

Plain text with **bold**, *italic*, ~~strikethrough~~, `inline code` and a
[link](https://github.com/porohkun/Beresta.Markdown.Wpf).

> A quote stays a quote.

```csharp
var editor = new MarkdownEditor();
```

| Mode | What is edited |
|---|---|
| View | nothing |
| Controls | tasks and collapsibles |
| Blocks | the blocks under the caret |
| Full | the whole source |
