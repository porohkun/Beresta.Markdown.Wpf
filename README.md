# Beresta.Markdown.Wpf v0.7.0

**English** | [Русский](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/README.ru.md)

A WPF control that shows Markdown. [Markdig](https://github.com/xoofx/markdig) parses the text, and
the viewer draws it with WPF controls and templates.

![The viewer in the showcase theme](https://raw.githubusercontent.com/wiki/porohkun/Beresta.Markdown.Wpf/images/overview.png)

## Why this viewer

- **Its own fast native rendering.** Markdown becomes real WPF controls, with no browser and no HTML
  in between — [How It Renders](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/How-It-Renders).
- **Full virtualization.** Only the blocks you see are created: in a `ScrollViewer`, on a page next to
  other content, inside a list of messages — [Scrolling and Virtualization](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Scrolling-and-Virtualization).
- **Selection like in a document.** One selection runs across paragraphs, lists, tables and code, even
  through blocks out of view; copying puts plain text, Markdown and HTML on the clipboard —
  [Selection](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Selection), [Copying](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Copying).
- **Easy to extend.** Your own syntax becomes your own widgets, and built-in syntax can be taken over —
  [Extensions](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Extensions).
- **Easy to restyle.** Every color, font and margin is a resource key, and any kind of block can get
  your own template — [Theming](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Theming), [Styles and Templates](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Styles-and-Templates).
- **Many Markdown elements, only the ones you need.** CommonMark, Markdig extensions and HTML from
  READMEs out of the box; the syntax of GitHub, GitLab, Obsidian, MkDocs, Docusaurus, HackMD and chats
  as ready-made extensions, one by one or as sets — [Supported Markdown](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Supported-Markdown).

## Install

```bash
dotnet add package Beresta.Markdown.Wpf
```

Requires .NET 7 or later (`net7.0-windows`) with WPF.

IntelliSense documentation comes in English and Russian: Visual Studio with a Russian interface shows
the Russian one.

## Connect the theme

Merge the theme of the viewer into the application resources once — without it the viewer stays
empty.

`App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/Beresta.Markdown.Wpf;component/Themes/Generic.xaml" />
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
        xmlns:mdv="clr-namespace:Beresta.Markdown.Wpf;assembly=Beresta.Markdown.Wpf"
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

`MainWindow.xaml` (`xmlns:kit="clr-namespace:Beresta.Markdown.Wpf.Kit;assembly=Beresta.Markdown.Wpf"` on the
root element):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:ObsidianSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Documentation

The [wiki](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki) describes every feature with a working example; all settings and hooks are
on one page, [Settings and Hooks](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Settings-and-Hooks).

- **Basics:** [Getting Started](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Getting-Started),
  [How It Renders](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/How-It-Renders),
  [Supported Markdown](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Supported-Markdown),
  [Settings and Hooks](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Settings-and-Hooks),
  [Scrolling and Virtualization](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Scrolling-and-Virtualization),
  [Comparison](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Comparison).
- **Working with text:** [Selection](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Selection),
  [Copying](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Copying),
  [Links and Anchors](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Links-and-Anchors),
  [Marks and Search](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Search-Highlighting),
  [Accessibility](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Accessibility).
- **Appearance:** [Theming](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Theming),
  [Styles and Templates](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Styles-and-Templates),
  [Code Blocks](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Code-Blocks),
  [Line Numbers](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Line-Numbers).
- **Integration:** [Application Links](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Application-Links),
  [Images](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Images),
  [Untrusted Text](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Untrusted-Text),
  [Task Lists](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Task-Lists),
  [Source Lines and Two Panes](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Source-Lines-and-Two-Panes),
  [Coordinates](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Coordinates),
  [Live Preview](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Live-Preview).
- **Your own syntax:** [Extensions](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Extensions).
- **Ready-made extensions:** [Sets](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Kit-Sets),
  [Callouts and Collapsibles](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Kit-Callouts),
  [Links and References](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Kit-Links),
  [Chips and Text Review](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Kit-Text),
  [Media and Data](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Kit-Media).

The repository also has a showcase application with these scenarios:

```bash
dotnet run --project Beresta.Markdown.Wpf.Showcase
```

## License

[MIT](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/LICENSE).
