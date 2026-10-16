**English** | [Русский](Task-Lists-ru)

A list item that starts with `[ ]` or `[x]` gets a check box instead of that text. By default the box
only shows the state. The viewer never changes the text by itself: a click on an enabled box asks your
application to do it.

**Any `.md` file**:

```markdown
- [x] take screenshots
- [ ] write the docs
```

## Toggling tasks

`CanToggleTasks="True"` enables the boxes. It is an inherited attached property: set it on a viewer, or
once on a window or a list of viewers. A click raises the bubbling routed event `TaskToggled` with the
task model `Task` and the wanted state `IsChecked`. `Task.Source` is the place of `[ ]` in the source,
so the application rewrites those three characters, and the new text updates the box.

**`MainWindow.xaml`**:

```xml
<ScrollViewer>
    <mdv:MarkdownViewer x:Name="Viewer"
                        CanToggleTasks="True"
                        TaskToggled="OnTaskToggled"
                        Margin="24,16" />
</ScrollViewer>
```

**`MainWindow.xaml.cs`**, in the window class:

```csharp
public MainWindow()
{
    InitializeComponent();
    Viewer.Markdown = "- [x] take screenshots\n- [ ] write the docs";
}

private void OnTaskToggled(object? sender, MdTaskToggledEventArgs e)
{
    var text = Viewer.Markdown ?? string.Empty;
    var source = e.Task.Source;
    Viewer.Markdown = text[..source.Start] + (e.IsChecked ? "[x]" : "[ ]") + text[source.End..];
}
```

With a binding, write the new text into your view model instead of `Viewer.Markdown`: setting the
property directly would replace the binding. The text is applied as edits, so the rest of the document
does not move — see [Live Preview](Live-Preview).

## Copying and the look

A task is copied as `[x]` or `[ ]` in plain text and as `- [x]` in Markdown. The box uses the
`CheckBox` style of your application; its margin is the key `Md.Task.Margin`, and the `TaskTemplate`
of a style provider replaces it — see [Styles and Templates](Styles-and-Templates).
