**English** | [Русский](Accessibility-ru)

The viewer describes the document to UI Automation — the interface that screen readers such as
Narrator and NVDA and testing tools use — with the same structure you see.

## What screen readers get

| On the screen | For UI Automation |
|---|---|
| the viewer | a `Document` |
| a paragraph, a table cell | `Text` with the paragraph's text |
| a heading | `Text` with a heading level from 1 to 6: readers can jump between headings |
| a list, a quote, a footnote | a `List` or a `Group` of the created content |
| a link | a `Hyperlink` that can be invoked |
| a footnote reference and `↩` | a `Hyperlink` named by the footnote number or `↩`; invoking it scrolls like a click |
| an image | an `Image` named by its alt text |
| a task | a `CheckBox`; unavailable until the application lets users toggle tasks |
| the copy button of a code block | a `Button` named by the theme key `Md.CodeBlock.CopyToolTip` |

The name of the viewer itself is yours to give: it is what a reader says when focus comes to the
document.

**A window or a user control**:

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}" AutomationProperties.Name="Release notes" />
```

Interface strings — the button name, the menu captions — are English by default; translate them with
theme keys: [Theming → Interface strings](Theming#interface-strings). An image gets a better name
from a meaningful alt text: `![Sales by month, 2025](sales.png)`.

## Keyboard

The viewer takes focus on a click and handles Ctrl+A, Ctrl+C and Ctrl+Shift+C. Tab moves between the
links of the created blocks, Enter opens the focused link. The context menu opens with the Menu key or
Shift+F10. Selecting text with the arrow keys is not supported.

## Limitation: only created blocks are visible

The viewer creates controls only for visible blocks — see
[Scrolling and Virtualization](Scrolling-and-Virtualization). UI Automation sees the same created
blocks: the text of blocks out of view is not in the automation tree until they are scrolled into
view, so «read all» stops at the end of the created part. A short text placed without a scrolling
container around it creates all its blocks and is visible to UI Automation in full.

To check what a reader gets, use Narrator or
[Accessibility Insights for Windows](https://accessibilityinsights.io/docs/windows/overview/).
