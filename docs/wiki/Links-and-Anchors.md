**English** | [Русский](Links-and-Anchors-ru)

Links in the viewer work like in a browser: web links open outside, anchors scroll inside the
document. Headings give a ready table of contents.

## What a click does

1. The viewer raises the routed event `MarkdownLink.Navigate`. If your application handles it, nothing
   else happens — see [Application Links](Application-Links).
2. A link to an anchor of this document (`#...`) scrolls to the anchor.
3. Everything else goes to the link opener. The default one opens addresses of the web and mail —
   `http`, `https`, `mailto` and `www.…` without a scheme — with the system handler. Other schemes
   and file paths are not opened: that is up to your application.

The title of a link, `[text](https://example.com "title")`, is its tooltip.

## Anchors

Every heading is an anchor. Its identifier is made like on GitHub: the text in lower case, where
letters, digits, `-` and `_` stay, spaces become `-` and other characters are dropped; a repeated
heading gets `-1`, `-2`; `MdTextMap.AnchorOf(text)` makes the identifier of a heading with this text.
An explicit identifier is set with attributes: `## Setup {#setup}`. Footnotes are anchors too:
`fn:1` is the first footnote, `fnref:1` the first reference to a footnote.

**Any `.md` file**:

```markdown
See [installation](#installation) or [the details](#advanced).

## Installation

...

## Advanced setup {#advanced}
```

## Scrolling to a place

| Method | Where it scrolls |
|---|---|
| `ScrollToAnchor(id, fraction)` | to a heading or a footnote; `false` — no such anchor |
| `ScrollToRange(range, fraction)` | to a range of the [text map](Selection#positions-the-text-map) |
| `ScrollToSourceLine(line, fraction)` | to a source line — see [Source Lines and Two Panes](Source-Lines-and-Two-Panes) |

`fraction` is where the target stands in the visible area: `0` — at the top, `0.5` — in the middle.
The target may be far out of view, among blocks that are not created yet. The viewer moves the
scrolling around it by pixels, so in a list set `VirtualizingPanel.ScrollUnit="Pixel"` — see
[Scrolling and Virtualization](Scrolling-and-Virtualization).

**`MainWindow.xaml.cs`**, for example in a button handler:

```csharp
Viewer.ScrollToAnchor("installation", 0.1);
```

## Table of contents

`TextMap.Headings` lists the headings of the document: `Level`, `Text`, anchor `Id` and the link to it
`Href` (`#id`), `Range` in the text map and `SourceLine`. A list bound to it is a table of contents;
`ViewportChanged` and `GetVisibleSourceLine()` tell which heading is being read now. A table of
contents right in the document, from `[TOC]`, is a [ready-made extension](Kit-Links#table-of-contents).

**`MainWindow.xaml`**:

```xml
<DockPanel>
    <ListBox x:Name="Contents"
             DockPanel.Dock="Left"
             Width="220"
             ItemsSource="{Binding TextMap.Headings, ElementName=Viewer}"
             DisplayMemberPath="Text"
             SelectionChanged="OnContentsSelectionChanged" />
    <ScrollViewer>
        <mdv:MarkdownViewer x:Name="Viewer"
                            Markdown="{Binding Text}"
                            ViewportChanged="OnViewportChanged"
                            Margin="24,16" />
    </ScrollViewer>
</DockPanel>
```

**`MainWindow.xaml.cs`**, in the window class (`using MarkdigWpfViewer.Selection;`):

```csharp
private bool _followingScroll;

private void OnContentsSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (!_followingScroll && Contents.SelectedItem is MdHeading heading)
    {
        Viewer.ScrollToRange(heading.Range);
    }
}

private void OnViewportChanged(object? sender, EventArgs e)
{
    if (Viewer.GetVisibleSourceLine() is not { } line)
    {
        return;
    }

    _followingScroll = true;
    Contents.SelectedItem = Viewer.TextMap.Headings.LastOrDefault(heading => heading.SourceLine <= line + 0.5);
    _followingScroll = false;
}
```

The list updates by itself when the text changes: a new text builds a new map. To indent nested
headings, bind the margin of an item to `Level` through a converter.
