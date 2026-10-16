**English** | [Русский](Getting-Started-ru)

The viewer is a library: you add it to a WPF application, connect its theme once and put
`MarkdownViewer` where the document should be.

Requirements: .NET 10 (`net10.0-windows`) with WPF.

## Install

As a NuGet package.

**Terminal**, in the folder of your application project:

```bash
dotnet add package Markdig.Wpf.Viewer
```

Or as a Git submodule with a project reference — handy when you change the viewer together with your
application.

**Terminal**, in the root of your repository:

```bash
git submodule add https://github.com/porohkun/Markdig.Wpf.Viewer.git external/Markdig.Wpf.Viewer
dotnet add YourApp/YourApp.csproj reference external/Markdig.Wpf.Viewer/Markdig.Wpf.Viewer/Markdig.Wpf.Viewer.csproj
```

## Connect the theme

The viewer draws blocks with the templates of its theme, `Themes/Generic.xaml`. WPF does not pick the
theme up by itself: merge it into the application resources once. Without it the viewer stays empty.

**`App.xaml`**, the application resources:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/Markdig.Wpf.Viewer;component/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

Your own dictionary that overrides theme keys goes **after** this one — see [Theming](Theming).

## Show a document

The viewer has no scroll bars of its own: put it into a `ScrollViewer`. Other places for it are in
[Scrolling and Virtualization](Scrolling-and-Virtualization).

**`MainWindow.xaml`**, the whole window:

```xml
<Window x:Class="YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mdv="clr-namespace:MarkdigWpfViewer;assembly=Markdig.Wpf.Viewer"
        Title="Markdown" Width="800" Height="600">
    <ScrollViewer>
        <mdv:MarkdownViewer x:Name="Viewer" Margin="24,16" />
    </ScrollViewer>
</Window>
```

**`MainWindow.xaml.cs`**, in the constructor after `InitializeComponent()`:

```csharp
Viewer.Markdown = """
    # Hello

    This is **Markdown** in a *WPF* window.

    - [x] theme connected
    - [ ] read the wiki
    """;
```

With MVVM bind the text as usual: `Markdown="{Binding Text}"`. A new value is applied to the shown
document as edits, so a text that changes often is fine — see [Live Preview](Live-Preview).

The font and the text color come from your application: the viewer inherits them from the window like
any other control and sets only sizes, margins and accents.

## XAML namespaces

The examples in this wiki use these prefixes:

| Prefix | Namespace | What is there |
|---|---|---|
| `mdv` | `clr-namespace:MarkdigWpfViewer;assembly=Markdig.Wpf.Viewer` | `MarkdownViewer`, `MarkdownLink` |
| `s` | `clr-namespace:MarkdigWpfViewer.Styling;assembly=Markdig.Wpf.Viewer` | `MdStyleProvider` |
| `p` | `clr-namespace:MarkdigWpfViewer.Presenters;assembly=Markdig.Wpf.Viewer` | controls for block templates |
| `vm` | `clr-namespace:MarkdigWpfViewer.ViewModels;assembly=Markdig.Wpf.Viewer` | models of blocks and inlines |
| `mdx` | `clr-namespace:MarkdigWpfViewer.Extensions;assembly=Markdig.Wpf.Viewer` | `MdWidget`, `MdExtensionCollection` |
| `sel` | `clr-namespace:MarkdigWpfViewer.Selection;assembly=Markdig.Wpf.Viewer` | `MdSelection` |
| `b` | `clr-namespace:MarkdigWpfViewer.Behaviors;assembly=Markdig.Wpf.Viewer` | `NestedScroll` |
| `local` | `clr-namespace:YourApp` | your classes from the examples |

The C# namespaces are the same: `MarkdigWpfViewer`, `MarkdigWpfViewer.Selection` and so on.
