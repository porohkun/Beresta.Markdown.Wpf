**English** | [Русский](Kit-Sets-ru)

A set connects the ready-made extensions of one program in one line: the syntax of GitHub, GitLab,
Obsidian, MkDocs, Docusaurus, HackMD or a chat. The order of the parts inside a set is fixed by the
library. A set goes into `Extensions` like a single extension.

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` on
the root element):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:ObsidianSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Sets

| Set | Parts, in order | Settings |
|---|---|---|
| [`GitHubSet`](Kit-Set-GitHub) | [properties card](Kit-Media#properties-card), [mentions](Kit-Links#mentions), [issue links](Kit-Links#issue-links), [color codes](Kit-Text#color-codes), [emoji](Kit-Media#emoji-shortcodes) | `Resolver`, `IssueTemplates` |
| [`GitLabSet`](Kit-Set-GitLab) | [table of contents](Kit-Links#table-of-contents), issue links, color codes, emoji | `IssueTemplates` |
| [`ObsidianSet`](Kit-Set-Obsidian) | properties card, [Obsidian callouts](Kit-Callouts#obsidian-callouts), [comments](Kit-Text#comments), [embedded images](Kit-Links#embedded-images), [wiki links](Kit-Links#wiki-links), [tags](Kit-Links#tags), [task statuses](Kit-Text#task-statuses) | `Resolver` |
| [`MkDocsSet`](Kit-Set-MkDocs) | [MkDocs admonitions](Kit-Callouts#mkdocs-admonitions) | — |
| [`DocusaurusSet`](Kit-Set-Docusaurus) | [`:::kind` containers](Kit-Callouts#containers-kind); fits VuePress too | — |
| [`HackMDSet`](Kit-Set-HackMD) | `:::kind` containers, table of contents, [shortcodes](Kit-Media#shortcodes) | `ShortcodeHandlers`, `LoadThumbnails` |
| [`ChatSet`](Kit-Set-Chat) | mentions, [spoilers](Kit-Text#spoilers), emoji | `Resolver` |

## Settings

The settings of a set are those of its parts: `Resolver` is the [resolver](Kit-Links#connecting) of
mentions or of wiki links and embeds, `IssueTemplates` are the templates of issue links,
`ShortcodeHandlers` and `LoadThumbnails` are those of shortcodes.

**`MainWindow.xaml`** (`xmlns:sys="clr-namespace:System;assembly=System.Runtime"` on the root element;
`local:TeamResolver` is your resolver):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:GitHubSet Resolver="{x:Static local:TeamResolver.Instance}">
            <kit:GitHubSet.IssueTemplates>
                <sys:String x:Key="#">https://github.com/org/repo/issues/{0}</sys:String>
            </kit:GitHubSet.IssueTemplates>
        </kit:GitHubSet>
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Switching parts off

`Without` names the parts a set does not connect — by the names of their extensions.

```xml
<kit:ObsidianSet Without="Comments, TaskStatuses" />
```

In code it is the `KitParts` flags (`using MarkdigWpfViewer.Kit;`):

```csharp
Viewer.Extensions.Add(new ObsidianSet { Without = KitParts.Comments | KitParts.TaskStatuses });
```

## Your own extensions in a set

A set is an [`MdExtensionSet`](Extensions#extension-sets): an extension added to its `Extensions` is
asked after the parts of the set.

**`MainWindow.xaml.cs`**, in the window constructor after `InitializeComponent()`
(`using MarkdigWpfViewer.Kit;`; `DiagramExtension` is your extension):

```csharp
var set = new GitHubSet();
set.Extensions.Add(new DiagramExtension());
Viewer.Extensions.Add(set);
```
