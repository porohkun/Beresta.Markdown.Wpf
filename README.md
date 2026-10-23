# Beresta.Markdown.Wpf v0.9.0

[![NuGet](https://img.shields.io/nuget/v/Beresta.Markdown.Wpf)](https://www.nuget.org/packages/Beresta.Markdown.Wpf)
[![Downloads](https://img.shields.io/nuget/dt/Beresta.Markdown.Wpf)](https://www.nuget.org/packages/Beresta.Markdown.Wpf)
[![Publish](https://img.shields.io/github/actions/workflow/status/porohkun/Beresta.Markdown.Wpf/nuget.yml?branch=main&label=publish)](https://github.com/porohkun/Beresta.Markdown.Wpf/actions/workflows/nuget.yml)
![.NET 7, 8, 9, 10](https://img.shields.io/badge/.NET-7%20%7C%208%20%7C%209%20%7C%2010-512BD4)
[![License](https://img.shields.io/github/license/porohkun/Beresta.Markdown.Wpf)](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/LICENSE)

**English** | [Русский](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/README.ru.md)

A fast native Markdown viewer for WPF, with no browser and no FlowDocument inside: documents are drawn
with real WPF controls and templates. It virtualizes huge documents, updates live for previews and
streamed chat answers, lets you select text across blocks, and is fully themeable and extensible.
[Markdig](https://github.com/xoofx/markdig) parses the text. The second package,
`Beresta.Markdown.Wpf.Editor`, edits the source right where it is drawn.

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
- **An editor in the same drawing.** The blocks under the caret show their Markdown in AvalonEdit, the
  rest of the document stays drawn; the source keeps the look of the drawing, from tables in columns to
  highlighted code, and the editor finds and replaces, pastes HTML as Markdown and edits front matter by
  type — [Editor](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Editor).

## Install

```bash
dotnet add package Beresta.Markdown.Wpf
```

Requires .NET 7 or later (`net7.0-windows`) with WPF.

IntelliSense documentation comes in English and Russian: Visual Studio with a Russian interface shows
the Russian one.

## Connect the theme

Merge the theme of the viewer into the application resources once — without it the viewer shows a hint
with this address instead of the document.

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
        xmlns:md="https://github.com/porohkun/Beresta.Markdown.Wpf"
        Title="Markdown" Width="800" Height="600">
    <ScrollViewer>
        <md:MarkdownViewer Markdown="{Binding Text}" Margin="24,16" />
    </ScrollViewer>
</Window>
```

The font and the text color come from your application.

## Syntax of other programs

Ready-made extensions draw the syntax of GitHub, GitLab, Obsidian, MkDocs, Docusaurus, HackMD and
chats. They are off by default; a set connects the syntax of one program.

`MainWindow.xaml` (`xmlns:md="https://github.com/porohkun/Beresta.Markdown.Wpf"` on the
root element):

```xml
<md:MarkdownViewer Markdown="{Binding Text}">
    <md:MarkdownViewer.Extensions>
        <md:ObsidianSet />
    </md:MarkdownViewer.Extensions>
</md:MarkdownViewer>
```

## Edit Markdown

The second package edits the source right where it is drawn. It brings the viewer of the same version,
AvalonEdit, and ReverseMarkdown for pasting HTML as Markdown.

```bash
dotnet add package Beresta.Markdown.Wpf.Editor
```

`App.xaml`, after the theme of the viewer:

```xml
<ResourceDictionary Source="pack://application:,,,/Beresta.Markdown.Wpf.Editor;component/Themes/Generic.xaml" />
```

`MainWindow.xaml`:

```xml
<ScrollViewer>
    <md:MarkdownEditor Markdown="{Binding Text, Mode=TwoWay}" EditMode="Blocks" Margin="24,16" />
</ScrollViewer>
```

Modes, keys, undo, front matter and pasting with formatting — [Editor](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Editor);
search and replace — [Marks and Search](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Search-Highlighting#search-and-replace-in-the-editor).

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
- **Editing:** [Editor](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Editor).
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
