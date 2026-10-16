# Markdig.Wpf.Viewer v0.4.0

**English** | [Русский](https://github.com/porohkun/Markdig.Wpf.Viewer/blob/main/README.ru.md)

A WPF control that shows Markdown as native WPF elements: [Markdig](https://github.com/xoofx/markdig)
parses the text, WPF templates draw it.

![The viewer in the showcase theme](https://raw.githubusercontent.com/porohkun/Markdig.Wpf.Viewer/main/docs/wiki/images/overview.png)

- **Virtualized.** The viewer lives in your own scrolling and creates only the blocks you see: a long
  document, a page with other content or a chat list stay fast.
- **Selectable.** Selection runs across blocks; copying puts plain text, Markdown and HTML on the
  clipboard at once.
- **Navigable.** Links, anchors, footnotes, a table of contents, scrolling to a source line; your
  application can take over any link.
- **Themable.** Every color, font and margin is a resource key; any block can get your own template.
- **Extensible.** Your own Markdown syntax becomes your own widgets.
- **Speaks other dialects.** Wiki links, callouts, mentions, shortcodes and tables from CSV of GitHub,
  GitLab, Obsidian, MkDocs, Docusaurus, HackMD and chats come with the package: off by default, a set
  connects the syntax of one program in one line.
- **Live.** A new text is applied as edits: fit for a live preview and streamed answers.

## Install

```bash
dotnet add package Markdig.Wpf.Viewer
```

Or add the repository as a Git submodule and reference `Markdig.Wpf.Viewer/Markdig.Wpf.Viewer.csproj`.

Requires .NET 10 (`net10.0-windows`) with WPF.

## Connect the theme

Merge the theme of the viewer into the application resources once — without it the viewer stays
empty.

`App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/Markdig.Wpf.Viewer;component/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## Show a document

The viewer has no scroll bars of its own: put it into a `ScrollViewer`.

`MainWindow.xaml`:

```xml
<Window x:Class="YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mdv="clr-namespace:MarkdigWpfViewer;assembly=Markdig.Wpf.Viewer"
        Title="Markdown" Width="800" Height="600">
    <ScrollViewer>
        <mdv:MarkdownViewer Markdown="{Binding Text}" Margin="24,16" />
    </ScrollViewer>
</Window>
```

The font and the text color come from your application.

## Syntax of other programs

Ready-made extensions draw the syntax of GitHub, GitLab, Obsidian, MkDocs, Docusaurus, HackMD and
chats. They are off by default; a set connects the syntax of one program.

`MainWindow.xaml` (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` on the
root element):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:ObsidianSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Documentation

The [wiki](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki) describes every feature with a working
example:

- **Basics:** [Getting Started](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Getting-Started),
  [Supported Markdown](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Supported-Markdown),
  [Scrolling and Virtualization](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Scrolling-and-Virtualization).
- **Working with text:** [Selection](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Selection),
  [Copying](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Copying),
  [Links and Anchors](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Links-and-Anchors),
  [Accessibility](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Accessibility).
- **Appearance:** [Theming](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Theming),
  [Styles and Templates](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Styles-and-Templates),
  [Code Blocks](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Code-Blocks),
  [Line Numbers](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Line-Numbers).
- **Extensibility and integration:** [Extensions](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Extensions),
  [Images](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Images),
  [Task Lists](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Task-Lists),
  [Application Links](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Application-Links),
  [Source Lines and Two Panes](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Source-Lines-and-Two-Panes),
  [Live Preview](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Live-Preview),
  [Search Highlighting](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Search-Highlighting).
- **Ready-made extensions:** [Sets](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Kit-Sets),
  [Callouts and Collapsibles](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Kit-Callouts),
  [Links and References](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Kit-Links),
  [Chips and Text Review](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Kit-Text),
  [Media and Data](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Kit-Media).

The repository also has a showcase application with these scenarios:

```bash
dotnet run --project Markdig.Wpf.Viewer.Showcase
```

## License

[MIT](https://github.com/porohkun/Markdig.Wpf.Viewer/blob/main/LICENSE).
