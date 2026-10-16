**English** | [Русский](Copying-ru)

Ctrl+C, the context menu and `Copy()` put the selection on the clipboard in three formats at once, and
the application you paste into takes the one it understands.

| Format | What is in it | Who takes it |
|---|---|---|
| `DataFormats.UnicodeText` | plain text, as on the screen | Notepad, terminals, plain text fields |
| `MdDataFormats.Markdown` (`text/markdown`) | the selection written as Markdown, UTF-8 bytes | your applications |
| `DataFormats.Html` | the selection as HTML | Word, mail, rich text editors |

The Markdown is written anew from the document model. A piece of one line keeps its inline formatting
and links; several blocks also keep headings, lists, quotes, code blocks and tables. Reference links
become inline ones, and `_italic_` becomes `*italic*`. A widget selected as a whole is written as its
source text.

**Your code that pastes** (`using System.IO; using System.Text; using MarkdigWpfViewer.Selection;`):

```csharp
if (Clipboard.GetData(MdDataFormats.Markdown) is MemoryStream stream)
{
    var markdown = Encoding.UTF8.GetString(stream.ToArray());
}
```

## Copy as Markdown

Ctrl+Shift+C, the menu item «Copy as Markdown» and `CopyMarkdown()` put the Markdown of the selection
as plain text: paste it into any text field, and the formatting survives as markup. The command behind
them is `MarkdownViewer.CopyMarkdownCommand`, so your own button can use it too.

**A window or a user control**:

```xml
<Button Content="Copy as Markdown"
        Command="{x:Static mdv:MarkdownViewer.CopyMarkdownCommand}"
        CommandTarget="{Binding ElementName=Viewer}" />
```

Without the clipboard, the same texts are in `SelectedText` and `SelectedMarkdown`.

## Changing what is copied

Before writing to the clipboard the viewer raises the standard `DataObject.Copying` event. A handler on
the viewer or on any of its ancestors gets the data with all three formats: it can add its own format
or cancel the copying. The copy button of a code block raises the same event.

**`MainWindow.xaml.cs`**, in the constructor after `InitializeComponent()`:

```csharp
DataObject.AddCopyingHandler(Viewer, (_, e) =>
{
    if (IsDocumentProtected)
    {
        e.CancelCommand();
        return;
    }

    e.DataObject.SetData("YourApp.DocumentId", DocumentId);
});
```

`IsDocumentProtected` and `DocumentId` stand for your own state.

## Giving Ctrl+C to the application

`IsCopyEnabled="False"` makes the viewer leave the `Copy` command alone: Ctrl+C goes up to your
command binding, and you decide what goes on the clipboard. `Copy()` and `CopyMarkdown()` still work
when you call them.

**`MainWindow.xaml`**:

```xml
<Window.CommandBindings>
    <CommandBinding Command="Copy" CanExecute="OnCanCopy" Executed="OnCopy" />
</Window.CommandBindings>

<ScrollViewer>
    <mdv:MarkdownViewer x:Name="Viewer" Markdown="{Binding Text}" IsCopyEnabled="False" />
</ScrollViewer>
```

**`MainWindow.xaml.cs`**, in the window class:

```csharp
private void OnCanCopy(object sender, CanExecuteRoutedEventArgs e)
    => e.CanExecute = Viewer.SelectionLength > 0;

private void OnCopy(object sender, ExecutedRoutedEventArgs e)
    => Clipboard.SetText(Viewer.SelectedMarkdown);
```

## Context menu

By default the viewer has a menu with «Copy», «Copy as Markdown» and «Select All». Its captions are
theme keys — translate them as described in [Theming](Theming#interface-strings).

Your own menu is the ordinary `ContextMenu` property. Point the commands at the viewer with
`CommandTarget`: otherwise they go to the element with keyboard focus.

**`MainWindow.xaml`**:

```xml
<mdv:MarkdownViewer x:Name="Viewer" Markdown="{Binding Text}">
    <mdv:MarkdownViewer.ContextMenu>
        <ContextMenu>
            <MenuItem Command="Copy"
                      CommandTarget="{Binding PlacementTarget, RelativeSource={RelativeSource AncestorType=ContextMenu}}" />
            <MenuItem Header="Search the web" Click="OnSearchWeb" />
        </ContextMenu>
    </mdv:MarkdownViewer.ContextMenu>
</mdv:MarkdownViewer>
```

**`MainWindow.xaml.cs`**, in the window class (`using System.Diagnostics;`):

```csharp
private void OnSearchWeb(object sender, RoutedEventArgs e)
{
    var query = Uri.EscapeDataString(Viewer.SelectedText);
    Process.Start(new ProcessStartInfo($"https://duckduckgo.com/?q={query}") { UseShellExecute = true });
}
```

To change the menu of every viewer, define your own resource with the key `Md.ContextMenu` and
`x:Shared="False"` in the application resources. `ContextMenu="{x:Null}"` on a viewer removes the menu.

## Code blocks

A code block has its own copy button that copies the whole code — see [Code Blocks](Code-Blocks).
