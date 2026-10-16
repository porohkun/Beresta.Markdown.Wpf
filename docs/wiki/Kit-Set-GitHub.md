**English** | [Русский](Kit-Set-GitHub-ru)

`GitHubSet` draws what GitHub adds to Markdown: front matter as a table, mentions, issue links, color
codes and emoji. Tables, task lists, `> [!NOTE]` callouts, footnotes and HTML from READMEs need no
set — the viewer draws them by itself, see [Supported Markdown](Supported-Markdown).

![A document in GitHub syntax](images/kit-github.png)

## Connecting

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` on
the root element):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:GitHubSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

`Resolver` gives the names of people, `IssueTemplates` the addresses of issues; without a template
`#123` stays text. How to set them is in [Sets](Kit-Sets#settings).

## Syntax

| Markdown | On the screen | Part |
|---|---|---|
| `---` … `---` at the start | a «key — value» table | [properties card](Kit-Media#properties-card) |
| `@name` | a chip with the name of the person | [mentions](Kit-Links#mentions) |
| `#123`, `org/repo#45` | a link by your template | [issue links](Kit-Links#issue-links) |
| `` `#0969DA` ``, `` `rgb(9, 105, 218)` `` | the code with a circle of its color | [color codes](Kit-Text#color-codes) |
| `:tada:` | an emoji | [emoji](Kit-Media#emoji-shortcodes) |

**Any `.md` file**:

```markdown
---
title: Release notes
version: 0.6.0
---

Thanks @anna for #12 and @boris for #15 :tada:

The accent color is `#0969DA`, the warning one is `rgb(154, 103, 0)`.
```
