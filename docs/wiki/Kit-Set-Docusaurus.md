**English** | [Русский](Kit-Set-Docusaurus-ru)

`DocusaurusSet` draws the `:::kind` containers of Docusaurus and VuePress: callouts and collapsible
blocks. MDX components and JSX are not drawn: they are shown as text.

![Containers in Docusaurus syntax](images/kit-docusaurus.png)

## Connecting

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` on
the root element):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:DocusaurusSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Syntax

| Markdown | On the screen |
|---|---|
| `:::tip Title` … `:::` | a callout of the kind; a container with an unknown name shows its content |
| `:::danger[Title]` … `:::` | a callout with the title of Docusaurus 3 |
| `:::details Title` … `:::` | a collapsible block, as in VuePress |

The kinds and their other names are in [Callouts and Collapsibles](Kit-Callouts#kinds).

**Any `.md` file**:

```markdown
:::tip Pro tip
Containers take a kind and a title.
:::

:::danger[A title in brackets]
As in Docusaurus 3.
:::

:::details Click to expand
A collapsible block from VuePress.
:::
```
