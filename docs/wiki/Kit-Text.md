**English** | [Русский](Kit-Text-ru)

Ready-made extensions for the look and the review of text: chips, color codes, CriticMarkup, hidden
comments, spoilers, task statuses and footnotes as tooltips. Off by default, connected one line each
(`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"`), like the
[callouts](Kit-Callouts#connecting).

![Chips, a color code, review marks, a spoiler and task statuses](images/kit-text.png)

**`MainWindow.xaml`**:

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:Chips />
        <kit:ColorChips />
        <kit:CriticMarkup />
        <kit:Comments />
        <kit:Spoilers />
        <kit:TaskStatuses />
        <kit:FootnoteTips />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Chips

`Chips`: `::text::` is a chip, `::text::{.kind}` a chip of a kind — `info`, `success`, `warning` or
`danger`; any other kind is a plain chip. A chip is styled text: selected by letters, found by search.

**Any `.md` file**:

```markdown
Status: ::done::{.success}, ::blocked::{.danger}, ::draft::.
```

## Color codes

`ColorChips`: inline code that is a whole color — `#RGB`, `#RRGGBB`, `#RRGGBBAA`, `rgb()`, `rgba()`,
`hsl()`, `hsla()` — gets a circle of that color next to it.

**Any `.md` file**:

```markdown
The accent is `#0969DA`, the hover is `rgba(9, 105, 218, 0.8)`.
```

## CriticMarkup

`CriticMarkup` draws review marks as styled text: `{++inserted++}`, `{--deleted--}`,
`{~~old~>new~~}`, `{==highlighted==}`; `{>>comment<<}` is an icon with the comment in a tooltip.

With `AllowDecisions="True"` every insertion, deletion and replacement gets «accept» and «reject»
buttons. The viewer never changes the text by itself: a click raises the bubbling event
`CriticMarkup.Decided` with the place of the change in the source, `Change`, and the text that
replaces it, `Replacement`. The application rewrites its text, as with [task lists](Task-Lists).

**`MainWindow.xaml`**:

```xml
<mdv:MarkdownViewer x:Name="Viewer" kit:CriticMarkup.Decided="OnDecided">
    <mdv:MarkdownViewer.Extensions>
        <kit:CriticMarkup AllowDecisions="True" />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

**`MainWindow.xaml.cs`**, in the window class (`using MarkdigWpfViewer.Kit;`):

```csharp
private void OnDecided(object sender, MdCriticDecidedEventArgs e)
{
    var text = Viewer.Markdown;
    Viewer.Markdown = text[..e.Change.Start] + e.Replacement + text[e.Change.End..];
}
```

## Comments

`Comments`: `%%comment%%` inside a line and a block between `%%` lines are not drawn.

**Any `.md` file**:

```markdown
Visible text %%and a hidden note%%.

%%
A hidden block
of several lines.
%%
```

## Spoilers

`Spoilers`: `||text||` is painted over until it is clicked; the click opens and closes it again, and
an opened spoiler stays open while the text changes. In a table row `|` divides the cells, so there is
no spoiler there.

## Task statuses

`TaskStatuses`: `- [/]` in progress, `- [-]` cancelled, `- [>]` moved, `- [?]` in question, `- [!]`
important — an icon instead of the box. A click raises the same `TaskToggled` as a
[task](Task-Lists#toggling-tasks), with `IsChecked = true`; `Task.Status` is the status character.

## Footnotes as tooltips

`FootnoteTips`: a footnote reference shows the footnote in a tooltip, and the definition is not drawn
where it is written.

## Look

| Key | What it is |
|---|---|
| `Md.Chip.Style`, `Md.Chip.Info.Style`, `Md.Chip.Success.Style`, `Md.Chip.Warning.Style`, `Md.Chip.Danger.Style` | `Span` styles of chips |
| `Md.ColorCode.Template` | the template of a color code; `Md.ColorCode.SwatchSize`, `.SwatchMargin`, `.SwatchBorder` |
| `Md.Critic.Insertion.Style`, `Md.Critic.Deletion.Style`, `Md.Critic.Highlight.Style` | `Span` styles of review marks |
| `Md.Critic.Comment.Template`, `Md.Critic.Decision.Template` | the comment icon and the decision buttons; `Md.Critic.CommentIcon`, `.CommentForeground`, `.AcceptIcon`, `.RejectIcon`, `.AcceptForeground`, `.RejectForeground` |
| `Md.Critic.AcceptToolTip`, `Md.Critic.RejectToolTip` | interface strings of the buttons |
| `Md.Spoiler.Style` | `Span` style of a spoiler; `Md.Spoiler.Cover`, `Md.Spoiler.RevealedBackground` |
| `Md.TaskStatus.Template`, `Md.TaskStatus.Style` | a task with a status; `Md.TaskStatus.Foreground`, `Md.TaskStatus.InProgress.Icon`, `.Cancelled.Icon`, `.Forwarded.Icon`, `.Question.Icon`, `.Important.Icon` |
| `Md.FootnoteTip.Template` | a footnote reference with a tooltip; `Md.FootnoteTip.MaxWidth` |

«Copy as Markdown» writes chips, review marks, spoilers and task statuses back in their own syntax.
