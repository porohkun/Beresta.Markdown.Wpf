**English** | [Русский](Kit-Set-MkDocs-ru)

`MkDocsSet` draws the admonitions of MkDocs and Material for MkDocs: `!!!` for a callout, `???` for a
collapsible one. Tables, footnotes, abbreviations, definition lists and task lists need no set.

![Admonitions in MkDocs syntax](images/kit-mkdocs.png)

## Connecting

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` on
the root element):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:MkDocsSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Syntax

| Markdown | On the screen |
|---|---|
| `!!! note "Title"` and content indented by four spaces | a callout; without a title — the title of the kind |
| `??? warning "Title"` | a collapsible callout, collapsed |
| `???+ example "Title"` | a collapsible callout, expanded |

The kinds and their other names are in [Callouts and Collapsibles](Kit-Callouts#kinds).

**Any `.md` file**:

```markdown
!!! note "Install"
    Run `dotnet add package Markdig.Wpf.Viewer`.

??? warning "Collapsed at first"
    Click the title to read.

???+ example "Open at first"
    A collapsible admonition that starts open.
```
