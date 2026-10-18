---
title: Ready-made extensions
tags: [showcase, extensions]
author:
  name: Showcase
  url: https://github.com/porohkun/Markdig.Wpf.Viewer
---

# Ready-made extensions

Syntax of other programs that the library knows by itself. It is off by default; all the ready-made
extensions are connected here. The table of contents below is an extension too: the paragraph `[TOC]`. The
properties card above is the front matter of this document.

[TOC]

## Links

Wiki links: [[Showcase]], [[Extensions#Chips|a caption instead of the name]], [[Missing page]] — the
resolver of the showcase does not know such a page, — and [[#Obsidian callouts]] to a section of this document.

Tags #showcase and #links/nested are chips; `C#` and `#123` are not tags. Mentions @ivan and @maria come
with names from the resolver; the address a@b.com is not a mention. Issue #1 leads to GitHub by the
template of the showcase.

An embedded image: ![[banner.png|160]]

## Working on text

Chips: ::done::{.success}, ::in progress::{.info}, ::risk::{.warning}, ::stop::{.danger} and ::just a chip::.
Colors in code get a swatch: `#0969DA`, `rgb(26, 127, 55)`, `hsl(280, 60%, 55%)`.

CriticMarkup review marks: text {++added++}, {--removed--}, {~~replaced~>corrected~~} and
{==highlighted==}{>>the comment shows in a tooltip<<}.

A comment %%invisible%% is not drawn, nor is the block below.

%%
This block is a comment of several lines.
%%

A spoiler: ||click to reveal||.

- [/] a task in progress
- [>] moved
- [?] in question
- [!] important
- [-] cancelled
- [ ] an ordinary one
- [x] a done one

A footnote in a tooltip[^tip] — hover over the number.

[^tip]: The footnote definition is not drawn but shown in a tooltip.

## Obsidian callouts

> [!info] A title of its own after the kind
> Any kind goes: the known ones have their own color and icon, an unknown one is drawn as a note.

> [!faq]- Another name of a kind, collapsed at first
> `faq` is another name of the `question` kind: the color and icon of a question, the title is `Faq`.

> [!success]+ Collapsible, expanded
> > [!quote] Callouts nest
> > A quote inside a success.

> [!todo]
> - [ ] without a title — the title of the kind from the theme captions

## MkDocs admonitions

!!! example "Example"
    The content is indented by four spaces, any markdown goes inside:

    ```csharp
    viewer.Extensions.Add(new MkDocsAdmonitions());
    ```

??? bug "Collapsed — three question marks"
    Visible once expanded.

???+ danger "Expanded — three question marks and a plus"
    Collapses with a click on the title.

## Containers `:::kind`

:::tip A tip from Docusaurus
A known name is a callout, the title goes after the name.
:::

:::warning[A title in brackets, as in Docusaurus 3]
A callout too.
:::

:::details Details from VuePress
A collapsible block.
:::

:::spoiler {state="open"} A HackMD spoiler, expanded
`spoiler` is a collapsible block too.
:::

## Media and data

An image link to a video becomes a card with a preview; a click opens the video:

[![Me at the zoo](https://img.youtube.com/vi/jNQXAC9IVRw/hqdefault.jpg)](https://www.youtube.com/watch?v=jNQXAC9IVRw)

An image with a Vimeo address is a card without a preview: ![Big Buck Bunny](https://vimeo.com/1084537)

Hugo and Jekyll shortcodes are widgets by name. The `kbd` name is registered by the showcase: {{< kbd Ctrl+Shift+P >}}.
An unknown shortcode {{< unknown >}} stays text.

{{< youtube jNQXAC9IVRw "Me at the zoo — by a shortcode" >}}

{% pdf "https://example.com/guide.pdf" %}

Emoji shortcodes: :rocket: :tada: :white_check_mark:; the smiley :) stays text.

A `csv` code block is a table, the first row is the header:

```csv
City,Population,"Area, km²"
Moscow,13 010 112,2561
Saint Petersburg,5 601 911,1439
Novosibirsk,1 633 595,502
```
