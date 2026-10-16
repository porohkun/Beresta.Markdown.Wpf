**English** | [Русский](Kit-Set-HackMD-ru)

`HackMDSet` draws the notes of HackMD and CodiMD: `:::kind` containers, a table of contents and the
embeds `{%youtube id %}`, `{%vimeo id %}`, `{%gist user/id %}`, `{%pdf address %}`.

![A note in HackMD syntax](images/kit-hackmd.png)

## Connecting

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` on
the root element):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:HackMDSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

`LoadThumbnails="False"` keeps video previews off the network. Other embeds of HackMD —
`{%slideshare %}`, `{%speakerdeck %}` — stay text until your application adds them to
`ShortcodeHandlers`, as in [shortcodes](Kit-Media#shortcodes).

## Syntax

| Markdown | On the screen | Part |
|---|---|---|
| `:::success`, `:::info`, `:::warning`, `:::danger` … `:::` | a callout of the kind | [`:::kind` containers](Kit-Callouts#containers-kind) |
| `:::spoiler Title` … `:::` | a collapsible block; `{state="open"}` after the name opens it | [`:::kind` containers](Kit-Callouts#containers-kind) |
| `[TOC]` as a paragraph | the headings of the document | [table of contents](Kit-Links#table-of-contents) |
| `{%youtube id %}`, `{%vimeo id %}` | a video card | [shortcodes](Kit-Media#shortcodes) |
| `{%gist user/id %}`, `{%pdf address %}` | a link card | [shortcodes](Kit-Media#shortcodes) |

**Any `.md` file**:

```markdown
[TOC]

## Meeting notes

:::success
Shipped on Friday!
:::

{%youtube jNQXAC9IVRw %}

:::spoiler Open questions
Hidden until expanded.
:::
```
