**English** | [Русский](Kit-Set-Chat-ru)

`ChatSet` draws the markup that chats share — Slack, Discord, Telegram: mentions, spoilers and emoji.
A chat is a list of short documents: put each message into its own viewer inside a virtualized list —
see [Scrolling and Virtualization](Scrolling-and-Virtualization).

![Messages in chat syntax](images/kit-chat.png)

## Connecting

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` on
the root element):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:ChatSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

`Resolver` gives the names of people; a click on a mention goes to your application as
`mention:name` — see [Links and References](Kit-Links).

## Syntax

| Markdown | On the screen | Part |
|---|---|---|
| `@name` | a chip with the name of the person | [mentions](Kit-Links#mentions) |
| `\|\|spoiler\|\|` | text painted over until it is clicked | [spoilers](Kit-Text#spoilers) |
| `:smile:` | an emoji | [emoji](Kit-Media#emoji-shortcodes) |

**Any `.md` file**:

```markdown
@anna the build is green :white_check_mark:

The answer is ||42|| :wink:
```
