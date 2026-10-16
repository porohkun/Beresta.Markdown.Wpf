**English** | [Русский](Extensions-ru)

An extension teaches the viewer your own syntax: it adds a construct to the Markdig parser, turns the
parsed nodes into models of your own and gives those models templates. Such a model with its template
is a **widget**. Built-in syntax can be taken over the same way.

![An inline widget and a block widget](images/widgets.png)

## Anatomy

An extension derives from `MdExtension` and overrides what it needs:

| Member | What it does |
|---|---|
| `SetupPipeline(builder)` | adds Markdig extensions and parsers for your syntax |
| `ConvertBlock(block, context)` | turns a Markdig block into your block model; `null` — not yours |
| `ConvertInline(inline, context)` | turns a Markdig inline into your inline model; `null` — not yours |
| `SelectTemplate(item, container)` | returns the template of your model; `null` — not yours |
| `CreateState(node)` | creates the state of a widget that outlives its control; `null` — no state |

The viewer asks its extensions in order, before its own conversion and before the style provider, so
the first extension that answers wins. `MdConvertContext` converts the content of your node like the
rest of the document: `ConvertChildren` for nested blocks, `ConvertInlines` for inline content;
`GetSource` and `GetSpan` give the source text and position of a node.

Models derive from `BlockVm` or `InlineVm`. Those are records, so your models are immutable records
too.

## A block widget

A note: a custom container `:::note Title` with Markdown inside. Custom containers are already in the
default pipeline, so the extension only converts them.

**Any `.md` file**:

```markdown
:::note Your own block
A container with **markdown inside**.
:::
```

**`NoteVm.cs`**:

```csharp
using MarkdigWpfViewer.Selection;
using MarkdigWpfViewer.ViewModels;

public sealed record NoteVm(string Title, IReadOnlyList<BlockVm> Blocks) : BlockVm, IMdTextSource
{
    public void WriteText(MdTextWriter writer)
    {
        writer.Text(Title);
        writer.Text("\n");
        writer.Blocks(Blocks);
    }
}
```

**`NoteExtension.cs`**:

```csharp
using System.Windows;
using Markdig.Extensions.CustomContainers;
using Markdig.Syntax;
using MarkdigWpfViewer.Extensions;
using MarkdigWpfViewer.ViewModels;

public sealed class NoteExtension : MdExtension
{
    public DataTemplate? Template { get; set; }

    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
        => block is CustomContainer { Info: "note" } container
            ? new NoteVm(container.Arguments ?? string.Empty, context.ConvertChildren(container))
            : null;

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is NoteVm ? Template : null;
}
```

**`MainWindow.xaml`**, the viewer:

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <local:NoteExtension>
            <local:NoteExtension.Template>
                <DataTemplate>
                    <Border Margin="0,4,0,8" Padding="12,8" CornerRadius="4" BorderThickness="4,0,0,0"
                            BorderBrush="#326CF3" Background="#F0F4FD">
                        <StackPanel>
                            <TextBlock Text="{Binding Title}" FontWeight="SemiBold" Margin="0,0,0,4" />
                            <p:MdBlocksPresenter Blocks="{Binding Blocks}" />
                        </StackPanel>
                    </Border>
                </DataTemplate>
            </local:NoteExtension.Template>
        </local:NoteExtension>
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

`p:MdBlocksPresenter` draws the nested blocks with the same templates and virtualization as the
document. A container that no extension takes is drawn as its content.

## An inline widget with its own syntax

A mention `@name`. Markdig does not know this syntax, so the extension adds an inline parser to the
pipeline. The parser is plain Markdig: see the
[Markdig documentation](https://github.com/xoofx/markdig) for parsers of other kinds.

**`Mentions.cs`**:

```csharp
using System.Windows;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;
using MarkdigWpfViewer.Extensions;
using MarkdigWpfViewer.Selection;
using MarkdigWpfViewer.ViewModels;

public sealed class MentionInline : LeafInline
{
    public required string Name { get; init; }
}

public sealed class MentionParser : InlineParser
{
    public MentionParser()
    {
        OpeningCharacters = ['@'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var start = slice.Start;
        var length = 0;
        while (char.IsLetterOrDigit(slice.PeekChar(length + 1)))
        {
            length++;
        }

        if (length == 0)
        {
            return false;
        }

        var position = processor.GetSourcePosition(start, out var line, out var column);
        processor.Inline = new MentionInline
        {
            Name = slice.Text.Substring(start + 1, length),
            Span = new(position, position + length),
            Line = line,
            Column = column
        };

        slice.Start += length + 1;
        return true;
    }
}

public sealed class MentionMarkdigExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<MentionParser>())
        {
            pipeline.InlineParsers.Insert(0, new MentionParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
    }
}

public sealed record MentionVm(string Name) : InlineVm, IMdCopyText
{
    public string CopyText => "@" + Name;
}

public sealed class MentionExtension : MdExtension
{
    public DataTemplate? Template { get; set; }

    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<MentionMarkdigExtension>();

    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
        => inline is MentionInline mention ? new MentionVm(mention.Name) : null;

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is MentionVm ? Template : null;
}
```

**`MainWindow.xaml`**, inside `<mdv:MarkdownViewer.Extensions>`:

```xml
<local:MentionExtension>
    <local:MentionExtension.Template>
        <DataTemplate>
            <Border Padding="6,0" CornerRadius="8" Background="#326CF3">
                <TextBlock Text="{Binding Name, StringFormat='@{0}'}" Foreground="White" />
            </Border>
        </DataTemplate>
    </local:MentionExtension.Template>
</local:MentionExtension>
```

An inline widget stands in the line like one character: it is selected as a whole and pasted as its
`CopyText`.

## Text of a widget

How a widget takes part in [selection](Selection) and [copying](Copying) depends on its model:

| The model implements | In the text map | Plain text copy |
|---|---|---|
| `IMdTextSource` | its text and nested blocks, written by `WriteText`; nested blocks are selected like ordinary ones | that text |
| `IMdCopyText` | one character | `CopyText` |
| neither | one character | nothing |

When a widget is selected as a whole, «Copy as Markdown» writes its source text.

## Widget state

A widget's control is short-lived: it disappears when its block scrolls out of view and may be
re-created by a new text. State that must survive — collapsed or not, a selected tab — goes into an
object created by `CreateState`. The viewer keeps it by the identity of the model while the model
lives in the document, and the template reads it through the inherited attached property
`mdx:MdWidget.State`.

**`NoteState.cs`**:

```csharp
using System.ComponentModel;

public sealed class NoteState : INotifyPropertyChanged
{
    private bool _isExpanded = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }
}
```

**`NoteExtension.cs`**, one more override:

```csharp
public override object? CreateState(NodeVm node) => node is NoteVm ? new NoteState() : null;
```

**`MainWindow.xaml`**, the note template; `BooleanToVisibility` is a `BooleanToVisibilityConverter` in
the window resources:

```xml
<DataTemplate>
    <StackPanel Margin="0,4,0,8">
        <ToggleButton Content="{Binding Title}"
                      HorizontalAlignment="Left"
                      IsChecked="{Binding (mdx:MdWidget.State).IsExpanded, RelativeSource={RelativeSource Self}}" />
        <p:MdBlocksPresenter Blocks="{Binding Blocks}"
                             Visibility="{Binding (mdx:MdWidget.State).IsExpanded, RelativeSource={RelativeSource Self}, Converter={StaticResource BooleanToVisibility}}" />
    </StackPanel>
</DataTemplate>
```

## Overriding built-in syntax

Extensions see the Markdig nodes before the viewer, so they can take over any built-in construct.

### Footnotes as tooltips

The viewer draws a footnote definition where it is written. To show it at the reference instead, the
extension turns each reference into a widget with the footnote text in a tooltip, and each definition
into an empty block.

**`FootnoteTips.cs`**:

```csharp
using System.Windows;
using Markdig.Extensions.Footnotes;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdigWpfViewer.Extensions;
using MarkdigWpfViewer.Selection;
using MarkdigWpfViewer.ViewModels;

public sealed record FootnoteTipVm(int Order, string Markdown) : InlineVm, IMdCopyText
{
    public string CopyText => $"[{Order}]";
}

public sealed record HiddenBlockVm : BlockVm, IMdTextSource
{
    public void WriteText(MdTextWriter writer)
    {
    }
}

public sealed class FootnoteTipExtension : MdExtension
{
    private static readonly DataTemplate Nothing = new();

    public DataTemplate? TipTemplate { get; set; }

    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
        => inline is FootnoteLink { IsBackLink: false } link
            ? new FootnoteTipVm(link.Footnote.Order, string.Join("\n\n", link.Footnote.Select(context.GetSource)))
            : null;

    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
        => block is Footnote ? new HiddenBlockVm() : null;

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item switch
        {
            FootnoteTipVm => TipTemplate,
            HiddenBlockVm => Nothing,
            _ => null
        };
}
```

**`MainWindow.xaml`**, the viewer; the tooltip shows the footnote with one more viewer:

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <local:FootnoteTipExtension>
            <local:FootnoteTipExtension.TipTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Order, StringFormat='[{0}]'}" Foreground="#326CF3" Cursor="Help">
                        <TextBlock.ToolTip>
                            <ToolTip MaxWidth="400">
                                <mdv:MarkdownViewer Markdown="{Binding Markdown}" />
                            </ToolTip>
                        </TextBlock.ToolTip>
                    </TextBlock>
                </DataTemplate>
            </local:FootnoteTipExtension.TipTemplate>
        </local:FootnoteTipExtension>
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

The blank lines around a definition stay as space.

### A code block of one language

The same way, blocks of one language become a widget — for example, diagrams drawn by your own
control:

```csharp
public sealed record DiagramVm(string Code) : BlockVm;

public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
    => block is FencedCodeBlock { Info: "mermaid" } code ? new DiagramVm(code.Lines.ToString()) : null;
```

## One set of extensions for many viewers

`Extensions` can be set by a style: all viewers of a window or an application then share one set.

**`App.xaml`**, the application resources, after the templates `NoteTemplate` and `MentionTemplate`:

```xml
<mdx:MdExtensionCollection x:Key="AppExtensions">
    <local:NoteExtension Template="{StaticResource NoteTemplate}" />
    <local:MentionExtension Template="{StaticResource MentionTemplate}" />
</mdx:MdExtensionCollection>

<Style TargetType="mdv:MarkdownViewer" BasedOn="{StaticResource {x:Type mdv:MarkdownViewer}}">
    <Setter Property="Extensions" Value="{StaticResource AppExtensions}" />
</Style>
```

Under such a style a viewer uses the shared set: extensions declared inside its own
`<mdv:MarkdownViewer.Extensions>` are replaced by it.

## Good to know

- Return the same `DataTemplate` instance for the same kind of model, not a new one per call: then a
  widget keeps its control when the text changes around it.
- A widget may change its height after it loads — an image, a diagram: the visible part of the
  document stays in place.
- Adding or removing an extension parses the document again and re-creates its blocks.
