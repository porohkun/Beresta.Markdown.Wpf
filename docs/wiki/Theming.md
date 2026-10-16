**English** | [Русский](Theming-ru)

Every color, size, margin and interface string of the viewer is a resource key `Md.<Block>.<Property>`.
The templates take them with `DynamicResource`, so a key overridden in the resources of the
application, a window or a viewer applies at once — even while the application runs.

![The same document with a dark set of keys](images/dark-theme.png)

## Overriding keys

A key defined closer to the viewer wins: the viewer's resources, then the window's, then the
application's. Inside one dictionary, its own keys win over its merged dictionaries.

**`App.xaml`**, the application resources (`xmlns:sys="clr-namespace:System;assembly=System.Runtime"`
on the root element):

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/Markdig.Wpf.Viewer;component/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>

        <SolidColorBrush x:Key="Md.Paragraph.LinkForeground" Color="#0969DA" />
        <SolidColorBrush x:Key="Md.CodeBlock.Background" Color="#F6F8FA" />
        <sys:Double x:Key="Md.Paragraph.FontSize">15</sys:Double>
    </ResourceDictionary>
</Application.Resources>
```

One viewer with smaller text:

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Resources>
        <sys:Double x:Key="Md.Paragraph.FontSize">12</sys:Double>
    </mdv:MarkdownViewer.Resources>
</mdv:MarkdownViewer>
```

A key keeps the type of its default value: sizes are `sys:Double`, margins and paddings `Thickness`,
colors `Brush`, `Md.List.MarkerWidth` and `Md.Footnote.MarkerWidth` are `GridLength`. The defaults are
in [`Themes/Generic.xaml`](https://github.com/porohkun/Markdig.Wpf.Viewer/blob/main/Markdig.Wpf.Viewer/Themes/Generic.xaml).

## Font and text color

The theme does not set the font and the color of the text: the viewer takes them from your
application, like a `TextBlock` does. Set `Md.Paragraph.FontFamily`, `Md.Paragraph.Foreground` and
their heading counterparts only when the text of the viewer must differ from the rest of the
application.

## Dark theme

A dark theme is one more dictionary with the same keys, merged after the theme of the viewer and
removed to go back. Keys defined right in `Application.Resources` win over any merged dictionary, so
keep the light values in a dictionary of their own too. The text color and the background come from
your window, so switch them there.

**`Themes/MarkdownDark.xaml`**, a resource dictionary in your application:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <SolidColorBrush x:Key="Md.Paragraph.LinkForeground" Color="#58A6FF" />
    <SolidColorBrush x:Key="Md.Heading.LinkForeground" Color="#58A6FF" />
    <SolidColorBrush x:Key="Md.InlineCode.Background" Color="#343942" />
    <SolidColorBrush x:Key="Md.InlineCode.Foreground" Color="#E6EDF3" />
    <SolidColorBrush x:Key="Md.InlineCode.BorderBrush" Color="#3D444D" />
    <SolidColorBrush x:Key="Md.CodeBlock.Background" Color="#161B22" />
    <SolidColorBrush x:Key="Md.CodeBlock.Foreground" Color="#E6EDF3" />
    <SolidColorBrush x:Key="Md.CodeBlock.BorderBrush" Color="#3D444D" />
    <SolidColorBrush x:Key="Md.Kbd.Background" Color="#161B22" />
    <SolidColorBrush x:Key="Md.Kbd.Foreground" Color="#E6EDF3" />
    <SolidColorBrush x:Key="Md.Kbd.BorderBrush" Color="#3D444D" />
    <SolidColorBrush x:Key="Md.Quote.BorderBrush" Color="#3D444D" />
    <SolidColorBrush x:Key="Md.Table.BorderBrush" Color="#3D444D" />
    <SolidColorBrush x:Key="Md.Table.HeaderBackground" Color="#161B22" />
</ResourceDictionary>
```

**`App.xaml.cs`**, in the application class:

```csharp
private ResourceDictionary? _dark;

public void SetDarkMarkdown(bool dark)
{
    if (dark && _dark is null)
    {
        _dark = new ResourceDictionary { Source = new Uri("pack://application:,,,/YourApp;component/Themes/MarkdownDark.xaml") };
        Resources.MergedDictionaries.Add(_dark);
    }
    else if (!dark && _dark is not null)
    {
        Resources.MergedDictionaries.Remove(_dark);
        _dark = null;
    }
}
```

## Interface strings

The captions of the context menu and the name of the code copy button are keys too, English by
default. Translate them in the application resources.

**`App.xaml`**, the application resources:

```xml
<sys:String x:Key="Md.ContextMenu.Copy">Копировать</sys:String>
<sys:String x:Key="Md.ContextMenu.CopyMarkdown">Копировать как Markdown</sys:String>
<sys:String x:Key="Md.ContextMenu.SelectAll">Выделить всё</sys:String>
<sys:String x:Key="Md.CodeBlock.CopyToolTip">Копировать код</sys:String>
```

## All keys

| Part | Keys |
|---|---|
| Paragraph | `Md.Paragraph.FontFamily`\*, `Md.Paragraph.Foreground`\*, `Md.Paragraph.FontSize`, `Md.Paragraph.LinkForeground` |
| Headings | `Md.Heading.FontFamily`\*, `Md.Heading.Foreground`\*, `Md.Heading.LinkForeground`; `Md.Heading1.FontSize`, `Md.Heading1.FontWeight`, `Md.Heading1.Margin` and the same for levels 2–6 |
| Inline code | `Md.InlineCode.FontFamily`, `.FontSize`, `.Foreground`, `.Background`, `.BorderBrush`, `.BorderThickness`, `.Padding`, `.CornerRadius` |
| Keys, `<kbd>` | `Md.Kbd.FontFamily`, `.Foreground`, `.Background`, `.BorderBrush`, `.BorderThickness`, `.Padding`, `.CornerRadius` |
| Code block | `Md.CodeBlock.FontFamily`, `.FontSize`, `.Foreground`, `.Background`, `.BorderBrush`, `.BorderThickness`, `.Padding`, `.CornerRadius`, `.LineNumberForeground`, `.LineNumberMargin` |
| Code block header | `Md.CodeBlock.HeaderFontFamily`\*, `.HeaderForeground`, `.HeaderFontSize`, `.HeaderMargin` |
| Code copy button | `Md.CodeBlock.CopyToolTip`, `Md.CodeBlock.CopyIcon`, `Md.CodeBlock.CopiedIcon` (both `Geometry`) |
| Quote | `Md.Quote.BorderBrush`, `.BorderThickness`, `.Padding`, `.Margin` |
| List | `Md.List.Margin`, `Md.List.MarkerWidth`, `Md.List.ItemSpacing` |
| Table | `Md.Table.BorderBrush`, `.BorderThickness`, `.CellPadding`, `.HeaderBackground` |
| Marked text, abbreviation | `Md.Marked.Background`, `Md.Abbreviation.UnderlineBrush` |
| Figure | `Md.Figure.Margin`, `.CaptionForeground`, `.CaptionFontSize` |
| Footer | `Md.Footer.Margin`, `.Foreground`, `.FontSize` |
| Image | `Md.Image.AltForeground` |
| Task | `Md.Task.Margin` |
| Footnote | `Md.Footnote.MarkerWidth`, `Md.Footnote.MarkerForeground`, `Md.Footnote.LinkFontSize` |
| Rule, blank line | `Md.HorizontalRule.Margin`, `Md.BlankLine.Height` |
| Line numbers | `Md.Gutter.FontFamily`\*, `Md.Gutter.Foreground`, `.FontSize`, `.Padding` |
| Selection | `Md.Selection.Brush` |
| Context menu | `Md.ContextMenu` (the menu itself), `Md.ContextMenu.Copy`, `.CopyMarkdown`, `.SelectAll` |

\* Not set by default: the value comes from the application.

Whole styles of blocks and inline formatting are keys too — see
[Styles and Templates](Styles-and-Templates).
