**English** | [Русский](Kit-Links-ru)

Ready-made extensions for links between documents and people: wiki links, embedded images, tags,
mentions, issue links and a table of contents in place. They are off by default and connected one
line each, like the [callouts](Kit-Callouts#connecting).

The meaning comes from your application. A click on a wiki link, a tag or a mention goes the way of
[links](Application-Links): the `MarkdownLink.Navigate` event gets the target `wiki:Page#Section`,
`tag:name` or `mention:name`, and your handler opens what it means. Whether a page exists and what
a person is called, the extension asks a resolver — `IMdLinkResolver`, implemented by your
application.

## Connecting

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` and
`xmlns:sys="clr-namespace:System;assembly=System.Runtime"` on the root element; `local:NotesResolver`
is your resolver):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}" mdv:MarkdownLink.Navigate="OnMarkdownLink">
    <mdv:MarkdownViewer.Extensions>
        <kit:WikiLinks Resolver="{x:Static local:NotesResolver.Instance}" />
        <kit:WikiEmbeds />
        <kit:Tags />
        <kit:Mentions Resolver="{x:Static local:NotesResolver.Instance}" />
        <kit:IssueLinks>
            <kit:IssueLinks.Templates>
                <sys:String x:Key="#">https://github.com/org/repo/issues/{0}</sys:String>
            </kit:IssueLinks.Templates>
        </kit:IssueLinks>
        <kit:TableOfContents />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

**`NotesResolver.cs`**:

```csharp
using MarkdigWpfViewer.Kit;

public sealed class NotesResolver : IMdLinkResolver
{
    public static NotesResolver Instance { get; } = new();

    public bool PageExists(string page) => page is "Home" or "Roadmap";

    public string? DisplayName(string user) => user == "anna" ? "Anna Smith" : null;
}
```

Both members have defaults — every page exists, a name stays as written — so implement only what
you need.

**`MainWindow.xaml.cs`**, in the window class:

```csharp
private void OnMarkdownLink(object sender, MarkdownLinkEventArgs e)
{
    if (e.Href.StartsWith("wiki:") || e.Href.StartsWith("tag:") || e.Href.StartsWith("mention:"))
    {
        MessageBox.Show(e.Href);
        e.Handled = true;
    }
}
```

## Wiki links

`WikiLinks`: `[[Page]]`, `[[Page#Section]]`, `[[Page|caption]]`. The link is styled text: it is
selected by letters and found by search. A page the resolver does not know is drawn with its own
look; `[[#Section]]` leads to a heading of this document.

**Any `.md` file**:

```markdown
See [[Roadmap]], [[Roadmap#Stage 2|the next stage]] and [[#Links]].
```

## Embedded images

`WikiEmbeds`: `![[file.png]]` is an image loaded by the [image loader](Images#where-images-come-from)
of the viewer; `![[file.png|200]]` and `![[file.png|200x100]]` give its size in pixels, any other
caption is its alt text. An embedded file that is not an image becomes a wiki link to it.

## Tags

`Tags`: `#tag` and `#tag/nested` are chips with the target `tag:tag`. Not a tag: digits only — that
is an issue, — `#` right after a letter or a digit, as in `C#`, and `#` inside a link or code.

## Mentions

`Mentions`: `@name` is a chip with the target `mention:name` and the name from the resolver. Not a
mention: `@` right after a letter or a digit, as in an e-mail address, and `@` inside a link. The chip
is the template `Md.Mention.Template` of the theme; with a transparent `Md.Mention.Background` it looks
like a link.

## Issue links

`IssueLinks` turns `#123`, `org/repo#45` and `PROJ-45` into links by your templates. A template is
the prefix exactly as it is written before the number — `#`, `org/repo#`, `PROJ-` — and the address
with `{0}` in place of the number. There are no templates by default, so nothing becomes a link until
you add one.

**`MainWindow.xaml.cs`**, in the constructor (`using MarkdigWpfViewer.Kit;`):

```csharp
var issues = new IssueLinks();
issues.Templates["#"] = "https://github.com/org/repo/issues/{0}";
issues.Templates["PROJ-"] = "https://jira.example.com/browse/PROJ-{0}";
Viewer.Extensions.Add(issues);
```

## Table of contents

`TableOfContents`: a paragraph of exactly `[TOC]`, `[[_TOC_]]` or `[[toc]]` becomes the list of the
headings of the document. An item leads to its heading by the `#id` link, and the list follows the
text as it changes.

## Look

| Key | What it is |
|---|---|
| `Md.WikiLink.Style`, `Md.WikiLink.Missing.Style` | `Span` styles of a wiki link and of a link to a page that does not exist |
| `Md.Tag.Style` | `Span` style of a tag |
| `Md.Mention.Template` | the template of a mention chip; `Md.Mention.Background`, `.Foreground`, `.Padding`, `.CornerRadius` |
| `Md.Toc.Template` | the template of the table of contents; `Md.Toc.Margin`, `Md.Toc.Level1.Margin` … `Md.Toc.Level6.Margin` |

«Copy as Markdown» writes wiki links, embeds, tags and issues back in their own syntax, a mention as
`@name`.
