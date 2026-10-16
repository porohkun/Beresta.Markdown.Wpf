**English** | [Русский](Line-Numbers-ru)

`ShowSourceLineNumbers` shows the numbers of the source lines in a margin on the left, like an editor
does. Numbers start from 1.

![Source line numbers and line numbers inside code](images/line-numbers.png)

**A window or a user control**:

```xml
<ScrollViewer>
    <mdv:MarkdownViewer Markdown="{Binding Text}"
                        ShowSourceLineNumbers="True"
                        ShowCodeLineNumbers="True"
                        Margin="8,16,24,16" />
</ScrollViewer>
```

## Which lines get a number

A number stands at every source line that has its own place on the screen: lines of text, blank
lines, the header of a code block and its lines of code. The closing fence of a code block, the
delimiter row of a table and the `===` or `---` under a heading have no place of their own and get no
number. The width of the margin is one for the whole document, so it does not jump while you scroll.

`ShowCodeLineNumbers` is a different numbering: from 1 inside every code block — see
[Code Blocks](Code-Blocks#line-numbers-inside-code).

## The look

The margin uses the keys `Md.Gutter.FontFamily`, `Md.Gutter.Foreground`, `Md.Gutter.FontSize` and
`Md.Gutter.Padding` — see [Theming](Theming). A template of the viewer of your own keeps the margin when
it has `p:MdLineGutter x:Name="PART_Gutter"`.

## Numbers and the source

The numbers are the line numbers of the text in `Markdown`, the same an editor shows for that text.
From code, lines are counted from 0 — see [Source Lines and Two Panes](Source-Lines-and-Two-Panes).
