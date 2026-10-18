---
title: Viewer showcase
tags: [sample, showcase]
---

# Viewer showcase

![Showcase banner](banner.png "An image from the showcase resources: the address is resolved against BaseUri")

This document is put together to check **embedding** and *a change of look*: the theme, fonts, your own
templates. Here is ~~everything~~ almost everything the viewer can draw, and `code in the middle of a line`.

## Text and links

An ordinary paragraph with [a link outside](https://github.com/porohkun/Markdig.Wpf.Viewer), the address
www.example.com without a scheme and ***bold italic***. Special characters: &copy; &amp; &laquo;quotes&raquo;.

The link [to the table](#table) leads inside the document, and [this link](https://github.com/porohkun/Markdig.Wpf.Viewer "The viewer repository")
has a tooltip.

A hard line break —  
right here.

Inline HTML: <kbd>Ctrl</kbd>+<kbd>C</kbd> copies, water is H<sub>2</sub>O, the area is m<sup>2</sup>,
a break by a tag<br>works too.<!-- and this comment is not visible -->

Indices without tags too: H~2~O and x^2^. Also ==a highlighter mark==, ++an insertion++ and the title
""War and Peace"". The abbreviation HTML shows its expansion.

*[HTML]: HyperText Markup Language

This sentence has a footnote[^showcase].

[^showcase]: A footnote is drawn where it is written; the arrow leads back to the reference.

### A third-level heading
#### Fourth
##### Fifth
###### Sixth

## Lists

- the first item
- the second item
  - nested
    - deeper still
- an item of two paragraphs

  the second paragraph of the same item

7) numbering from seven
8) and with a parenthesis

c. letters from the third
d. and on

iv. Roman numerals from the fourth
v. and on

- [ ] a task
- [x] a done task

## Definitions

Markdig
:   A markdown parser for .NET: gives the viewer the tree of the document.

Callout
Admonition
:   A block with the color, icon and title of its kind.
:   The kind is written in the first line of the quote.

## Quote

> A quote with **emphasis** and a list:
>
> - one
> - two

## Callouts

> [!NOTE]
> A note — what is worth knowing even when skimming.

> [!TIP]
> A tip — how to make it simpler.

> [!IMPORTANT]
> Important — you cannot do without this.

> [!WARNING]
> A warning — needs **attention** at once.

> [!CAUTION]
> Caution — the action has consequences.

## HTML from README

<p align="center">
  <img src="banner.png" width="240" alt="Showcase banner">
  <br>
  <b>Markdig.Wpf.Viewer</b> — markdown for WPF
</p>

<details>
<summary>A collapsible block — click to expand</summary>

Inside is ordinary **markdown**:

- an item
- one more item

</details>

<details open>
<summary>Expanded at once: the <code>open</code> attribute</summary>

Being collapsed survives scrolling and a new text.

</details>

<div align="right">

Text aligned to the right.

</div>

## Table

| Left | Center | Right |
|:------|:---------:|-------:|
| a     | b         | c      |
| `code` | **bold** | 10    |

A grid table holds several blocks in a cell:

+------------------+----------------+
| Cell             | Content        |
+==================+================+
| First paragraph  | - item         |
|                  | - another item |
| Second paragraph |                |
+------------------+----------------+

## Figure

^^^
![Showcase banner](banner.png)
^^^ A caption under the image

## Code

```csharp
public sealed class Example
{
    public string Name { get; } = "showcase";
}
```

---

The end of the document.

^^ A footer — in small text at the end.
