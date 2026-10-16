**English** | [Русский](Supported-Markdown-ru)

The viewer parses text with Markdig: CommonMark plus Markdig's advanced extensions
(`UseAdvancedExtensions()`) — tables, task lists, footnotes, heading identifiers, autolinks,
attributes, custom containers and more. Everything the parser recognizes gets on the screen: a construct
without its own look is shown as its content or as its source text. Nothing disappears silently.

![Most of the syntax in one document](images/overview.png)

## Blocks

| Markdown | On the screen |
|---|---|
| `# Heading` … `###### Heading`, underlined with `===` or `---` | six heading levels |
| a paragraph | wrapped text; a line break in the source is a line break on the screen |
| `> quote` | a bar on the left; any blocks inside |
| `- item`, `* item`, `1. item`, `7) item` | bullets by nesting level (`•`, `◦`, `▪`), numbers from the first item |
| `a. item`, `A. item`, `i. item`, `I. item` | letters and roman numerals, also from the first item: `c.` starts with c |
| `- [ ] task`, `- [x] task` | a check box before the item — see [Task Lists](Task-Lists) |
| ```` ```lang ```` fenced code | a code block with a language header — see [Code Blocks](Code-Blocks) |
| code indented by four spaces | a code block without a header |
| `\| a \| b \|` with a `\|---\|---\|` row | a table with a header row; `:---`, `:---:` and `---:` align columns |
| a grid table drawn with `+---+` and `\|` | a table whose cell can hold several paragraphs, a list or code |
| `---`, `***` | a horizontal rule |
| `[^1]: text` | a footnote definition — see below |
| `^^^` … `^^^ Caption` | a figure: its content centered and a caption under it |
| `^^ text` | a footer in small text |
| `:::name` … `:::` | a custom container: shows its content; an [extension](Extensions) can draw it its own way |
| an HTML block | its source text |
| `<!-- comment -->` | nothing |

## Inline

| Markdown | On the screen |
|---|---|
| `**bold**`, `*italic*`, `***both***` | font weight and style |
| `~~strike~~` | strikethrough |
| `H~2~O`, `x^2^` | subscript and superscript |
| `==marked==` | highlighter background |
| `++inserted++` | underline |
| `""Title of a work""` | its own style, italic by default |
| `*[HTML]: HyperText Markup Language` | every "HTML" in the document gets a tooltip with the expansion; the definition itself is not shown |
| `` `code` `` | code in a frame |
| `[text](https://example.com "title")` | a link; the title is its tooltip — see [Links and Anchors](Links-and-Anchors) |
| `<https://example.com>`, `www.example.com` | links |
| `![alt](picture.png)` | an image — see [Images](Images) |
| `[^1]` | a footnote reference |
| `&copy;`, `&laquo;` | the characters © and « |
| two spaces or `\` at the end of a line, `<br>` | a hard line break |

Inline HTML: `<b>`, `<strong>`, `<i>`, `<em>`, `<s>`, `<del>`, `<strike>`, `<code>`, `<kbd>`,
`<sub>`, `<sup>`, `<mark>`, `<ins>`, `<u>`, `<cite>`, `<a href="…" title="…">`,
`<img src="…" alt="…" width="…" height="…">` and `<br>` are drawn like their Markdown counterparts;
`<kbd>` is a key cap. Other tags are shown as text.

**Any `.md` file**:

```markdown
Press <kbd>Ctrl</kbd>+<kbd>C</kbd> to copy. Water is H<sub>2</sub>O, the area is 5 m<sup>2</sup>.
```

## Line breaks and blank lines

The viewer keeps the shape of the source. Every source line starts a new line on the screen: a soft
line break inside a paragraph is drawn as a break, not as a space. Each blank line between blocks
becomes vertical space. The document looks like its text, and that is what makes
[line numbers](Line-Numbers) and [two panes](Source-Lines-and-Two-Panes) line up with the source.

## Footnotes

A footnote definition stays where it is written: it is not moved to the end of the document. A click on
a reference scrolls to the definition, `↩` in the definition scrolls back to the reference.

**Any `.md` file**:

```markdown
The viewer is built on Markdig[^parser].

[^parser]: A fast, CommonMark-compliant Markdown parser for .NET.
```

To show footnotes your own way — for example, as a tooltip at the reference — take them over with an
extension: [Extensions → Overriding built-in syntax](Extensions#overriding-built-in-syntax).

## Your own parser settings

`MarkdownPipeline` replaces the default Markdig pipeline — for example, to switch syntax off. Viewer
[extensions](Extensions) are added on top of it.

**`MainWindow.xaml.cs`**, in the constructor (`using Markdig;`):

```csharp
Viewer.MarkdownPipeline = new MarkdownPipelineBuilder()
    .UsePipeTables()
    .UseTaskLists()
    .Build();
```

## Typographic replacements

SmartyPants replaces typewriter characters with typographic ones: straight quotes with curly ones,
`--` and `---` with dashes, `...` with an ellipsis, `<<` and `>>` with guillemets. It is off by default:
in technical texts it would turn `--force` into "–force". Switch it on in your own parser settings;
the table of replacements comes from there too, its values are HTML entities.

**`MainWindow.xaml.cs`**, in the constructor (`using Markdig; using Markdig.Extensions.SmartyPants;`):

```csharp
var typography = new SmartyPantOptions();
typography.Mapping[SmartyPantType.LeftDoubleQuote] = "&laquo;";
typography.Mapping[SmartyPantType.RightDoubleQuote] = "&raquo;";

Viewer.MarkdownPipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .UseSmartyPants(typography)
    .Build();
```

With this table `"Цитата"` is shown as «Цитата».
