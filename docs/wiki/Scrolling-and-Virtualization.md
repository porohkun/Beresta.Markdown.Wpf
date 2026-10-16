**English** | [Русский](Scrolling-and-Virtualization-ru)

`MarkdownViewer` has no scroll bars of its own. It lives inside the scrolling you already have and
creates controls only for the blocks that are visible through every scrolling container around it.
The other blocks exist as light models: selection, copying, search and the table of contents work with
the whole document anyway.

## In a ScrollViewer

The usual case: one document fills a pane.

**A window or a user control**:

```xml
<ScrollViewer>
    <mdv:MarkdownViewer Markdown="{Binding Text}" Margin="24,16" />
</ScrollViewer>
```

## As a part of a page

The viewer can stand on a page next to other content, with one scroll bar for everything. Only the
visible part of the document is created, even when the viewer is much taller than the window.

**A window or a user control**:

```xml
<ScrollViewer>
    <StackPanel Margin="24,16">
        <TextBlock Text="{Binding Title}" FontSize="24" />
        <mdv:MarkdownViewer Markdown="{Binding Body}" />
        <Button Content="Reply" HorizontalAlignment="Left" />
    </StackPanel>
</ScrollViewer>
```

## In a virtualized list

A chat or a feed: a viewer per message inside a virtualized list. The list creates only visible
messages, and a long message creates only its visible blocks.

**A window or a user control**:

```xml
<ItemsControl ItemsSource="{Binding Messages}"
              VirtualizingPanel.IsVirtualizing="True"
              VirtualizingPanel.ScrollUnit="Pixel"
              ScrollViewer.CanContentScroll="True">
    <ItemsControl.Template>
        <ControlTemplate TargetType="ItemsControl">
            <ScrollViewer CanContentScroll="True">
                <ItemsPresenter />
            </ScrollViewer>
        </ControlTemplate>
    </ItemsControl.Template>
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel />
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <mdv:MarkdownViewer Markdown="{Binding Text}" Margin="8" />
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

`VirtualizingPanel.ScrollUnit="Pixel"` scrolls smoothly through long messages instead of jumping by
whole items. It is also required for scrolling to a source line, a range or an anchor: the viewer moves
the scroll position by pixels.

## Without scrolling

Without a scrolling container around it, the viewer creates all blocks and takes the height of the
whole document. That is fine for short texts: a tooltip, a card, a dialog.

## Nested scrolling

Wide tables and code blocks scroll sideways on their own but pass the mouse wheel to the page, so the
document keeps scrolling while the pointer is over them; Shift + wheel scrolls them sideways. A widget
of yours with its own `ScrollViewer` gets the same behavior with one attached property.

**Your widget template** (see [Extensions](Extensions)):

```xml
<ScrollViewer HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Disabled"
              b:NestedScroll.PassWheelToParent="True">
    <TextBlock Text="{Binding Wide}" />
</ScrollViewer>
```

## New text keeps the view in place

A new `Markdown` value is applied as edits: unchanged blocks keep their controls and measured heights,
and the visible part does not jump when the text changes above it — see [Live Preview](Live-Preview).
