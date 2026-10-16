**English** | [Русский](Live-Preview-ru)

A new `Markdown` value does not rebuild the document: the viewer matches the new document with the
shown one and applies the difference as edits. Blocks that did not change, or changed in place, keep their
controls, measured heights and widget state; the selection stays while its blocks exist; the visible
part does not jump when the text changes above it. So the viewer fits a preview next to an editor and
answers that arrive piece by piece.

## Preview next to an editor

**`MainWindow.xaml`**:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition />
        <ColumnDefinition />
    </Grid.ColumnDefinitions>

    <TextBox x:Name="Editor"
             AcceptsReturn="True"
             AcceptsTab="True"
             TextWrapping="Wrap"
             VerticalScrollBarVisibility="Auto"
             FontFamily="Consolas" />

    <ScrollViewer Grid.Column="1">
        <mdv:MarkdownViewer Markdown="{Binding Text, ElementName=Editor}" Margin="24,16" />
    </ScrollViewer>
</Grid>
```

The viewer gets every keystroke. Every change parses the whole text, so for very long documents
update `Markdown` after a short pause in typing instead.

## A streamed answer

An answer that comes in chunks is shown by setting the whole text so far after each chunk. Only the
last block changes; the rest stays as it is.

**`MainWindow.xaml.cs`**, a button handler; the chunks here come from a string (`using System.Text;`):

```csharp
private async void OnAnswer(object sender, RoutedEventArgs e)
{
    const string answer = "## Answer\n\nThe viewer applies **every new text** as edits, so a streamed answer does not flicker.";

    var shown = new StringBuilder();
    foreach (var word in answer.Split(' '))
    {
        shown.Append(word).Append(' ');
        Viewer.Markdown = shown.ToString();
        await Task.Delay(40);
    }
}
```

## Identity of elements

Every element of the model has a `NodeId` that survives a new text while the element is unchanged or
changed in place. The viewer keeps controls and [widget state](Extensions#widget-state) by it. For a
widget to keep its control, its extension must return the same `DataTemplate` instance every time.
