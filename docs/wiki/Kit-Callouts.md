**English** | [Русский](Kit-Callouts-ru)

Ready-made extensions for callouts and collapsible blocks in the syntax of Obsidian, MkDocs,
Docusaurus, VuePress and HackMD. They come with the package, are off by default and are connected
with one line each.

## Connecting

The ready-made extensions live in the namespace `MarkdigWpfViewer.Kit`.

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` on
the root element):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:ObsidianCallouts />
        <kit:MkDocsAdmonitions />
        <kit:CalloutContainers />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

**`MainWindow.xaml.cs`**, in the constructor (`using MarkdigWpfViewer.Kit;`):

```csharp
Viewer.Extensions.Add(new ObsidianCallouts());
```

## Kinds

All three draw the same [callouts](Supported-Markdown#callouts). Besides the five GitHub kinds there
are ten more, each with its own color, icon and title — the theme keys `Md.Callout.<Kind>.Brush`,
`.Icon` and `.Title`, see [Theming → Callout kinds](Theming#callout-kinds). Letter case does not
matter. Another name of a kind gets the look of that kind and keeps itself as the title.

| Kind | Other names |
|---|---|
| `note` | — |
| `tip` | `hint` |
| `important` | — |
| `warning` | `attention` |
| `caution` | — |
| `abstract` | `summary`, `tldr` |
| `info` | — |
| `todo` | — |
| `success` | `check`, `done` |
| `question` | `help`, `faq` |
| `failure` | `fail`, `missing` |
| `danger` | `error` |
| `bug` | — |
| `example` | — |
| `quote` | `cite` |

Any other kind is drawn as a note titled with its name. Obsidian treats `important` and `caution` as
other names of `tip` and `warning`; here they keep their GitHub look.

## Obsidian callouts

`ObsidianCallouts` adds to the GitHub syntax what Obsidian adds: a title after the kind, and `-` or
`+` right after the kind for a callout that folds; `-` starts it folded. Callouts nest.

**Any `.md` file**:

```markdown
> [!faq]- Why is the sky blue?
> Because of Rayleigh scattering.

> [!example] Nested
> > [!quote]
> > A callout inside a callout.
```

## MkDocs admonitions

`MkDocsAdmonitions` reads `!!! kind "Title"` with the content indented by four spaces; `???` starts
a folded collapsible admonition, `???+` an expanded one. The title is plain text; without it, the
title of the kind is shown.

**Any `.md` file**:

```markdown
!!! warning "Breaking change"
    `Render` is gone, use `Draw`.

??? tip "More"
    Hidden until expanded.
```

## Containers `:::kind`

`CalloutContainers` reads the containers of Docusaurus, VuePress and HackMD: a known kind becomes a
callout, `details` and `spoiler` become collapsible blocks. The title follows the name or goes into
square brackets, as in Docusaurus 3; `{state="open"}` expands a HackMD spoiler at once. A container
with any other name shows its content, as without the extension.

**Any `.md` file**:

```markdown
:::tip Pro tip
Callouts cost nothing.
:::

:::details Click to expand
Hidden text.
:::
```

## Copy as Markdown

The callouts of all three extensions are copied in the Obsidian syntax — `> [!KIND]` with the fold
mark and the title; collapsible blocks — as `<details>` with `<summary>`.
