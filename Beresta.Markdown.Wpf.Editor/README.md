# Beresta.Markdown.Wpf.Editor v0.9.1

[![NuGet](https://img.shields.io/nuget/v/Beresta.Markdown.Wpf.Editor)](https://www.nuget.org/packages/Beresta.Markdown.Wpf.Editor)
[![Downloads](https://img.shields.io/nuget/dt/Beresta.Markdown.Wpf.Editor)](https://www.nuget.org/packages/Beresta.Markdown.Wpf.Editor)
[![Publish](https://img.shields.io/github/actions/workflow/status/porohkun/Beresta.Markdown.Wpf/nuget.yml?branch=main&label=publish)](https://github.com/porohkun/Beresta.Markdown.Wpf/actions/workflows/nuget.yml)
![.NET 7, 8, 9, 10](https://img.shields.io/badge/.NET-7%20%7C%208%20%7C%209%20%7C%2010-512BD4)
[![License](https://img.shields.io/github/license/porohkun/Beresta.Markdown.Wpf)](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/LICENSE)

**English** | [Русский](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/Beresta.Markdown.Wpf.Editor/README.ru.md)

A Markdown editor for WPF that edits the source right where it is drawn. `MarkdownEditor` is the
[Beresta.Markdown.Wpf](https://www.nuget.org/packages/Beresta.Markdown.Wpf) viewer with everything it can
do, plus the source in [AvalonEdit](https://github.com/icsharpcode/AvalonEdit): the blocks under the caret
show their Markdown, and the rest of the document stays drawn.

![The block under the caret shows its source](https://raw.githubusercontent.com/wiki/porohkun/Beresta.Markdown.Wpf/images/editor-blocks.png)

## What it does

- **Four modes.** `View` only shows; `Controls` lets task boxes, folding and the properties card write
  into the source; `Blocks` shows the blocks under the caret as their source; `Full` puts the whole
  source in place of the drawing.
- **The source looks like the drawing.** An edited block keeps the height of its drawing, so the text
  around it does not move; headings keep their size, code its highlighting, tables stand in columns.
- **Keys for Markdown.** Enter continues lists and quotes, Tab moves items, numbered lists renumber
  themselves, Ctrl+B and Ctrl+I frame bold and italic, a typed code fence closes itself.
- **Search and replace** in the source, with the matches marked in the drawing too.
- **Paste with formatting.** Ctrl+Shift+V turns the HTML of a browser or an office program into
  Markdown.
- **Front matter by type.** Flags, dates and lists of the properties card are written back the way they
  were written.

## Install

```bash
dotnet add package Beresta.Markdown.Wpf.Editor
```

Requires .NET 7 or later (`net7.0-windows`) with WPF. The package brings the viewer of the same version,
AvalonEdit, and ReverseMarkdown for pasting HTML as Markdown.

IntelliSense documentation comes in English and Russian: Visual Studio with a Russian interface shows
the Russian one.

## Connect the themes

Merge the theme of the viewer and, after it, the theme of the editor into the application resources
once.

`App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/Beresta.Markdown.Wpf;component/Themes/Generic.xaml" />
            <ResourceDictionary Source="pack://application:,,,/Beresta.Markdown.Wpf.Editor;component/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## Edit a document

The editor has no scroll bars of its own: put it into a `ScrollViewer`, like the viewer.

`MainWindow.xaml`:

```xml
<Window x:Class="YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="https://github.com/porohkun/Beresta.Markdown.Wpf"
        Title="Markdown" Width="800" Height="600">
    <ScrollViewer>
        <md:MarkdownEditor x:Name="Editor"
                           Markdown="{Binding Text, Mode=TwoWay}"
                           EditMode="Blocks"
                           Margin="24,16" />
    </ScrollViewer>
</Window>
```

The edited text goes to `Markdown` after every edit, and the binding hands it to your view model at
once; the drawing catches up a second after the last edit.

## Documentation

- [Editor](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Editor) — modes, keys, undo, front
  matter, pasting with formatting and the look of the source.
- [Marks and Search](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Search-Highlighting#search-and-replace-in-the-editor)
  — search and replace in the source.
- The [wiki](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki) of the viewer: the editor draws the
  document the way the viewer does, with the same themes, extensions and settings.

## License

[MIT](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/LICENSE).
