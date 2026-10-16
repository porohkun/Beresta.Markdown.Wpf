namespace MarkdigWpfViewer.Selection;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ViewModels;

/// <summary>
/// Разметка выделенного: выделенный кусок модели заново записывается markdown со своим
/// оформлением. Кусок одного текста — строчной разметкой, несколько блоков — вместе с разметкой
/// блоков; виджет расширения — своим исходником.
/// </summary>
internal sealed partial class MdMarkdownWriter
{
    private static readonly Marker Bold = new("**", "**", Emphasis: true);
    private static readonly Marker Italic = new("*", "*", Emphasis: true);
    private static readonly Marker Strike = new("~~", "~~", Emphasis: true);
    private static readonly Marker Marked = new("==", "==", Emphasis: true);
    private static readonly Marker Inserted = new("++", "++", Emphasis: true);
    private static readonly Marker Cite = new("\"\"", "\"\"", Emphasis: true);

    private readonly MdTextMap _map;
    private readonly string _source;
    private readonly MdTextRange _selection;
    private readonly Dictionary<BlockVm, MdTextRange> _ranges = new(ReferenceEqualityComparer.Instance);
    private readonly List<IReadOnlyList<InlineVm>> _texts = [];
    private readonly HashSet<IReadOnlyList<InlineVm>> _cells = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<IReadOnlyList<InlineVm>, CodeBlockVm> _codes = new(ReferenceEqualityComparer.Instance);
    private bool _other;

    private MdMarkdownWriter(MdTextMap map, string source, MdTextRange selection)
    {
        _map = map;
        _source = source;
        _selection = selection;

        foreach (var (block, range) in map.Blocks)
        {
            _ranges[block] = range;
        }
    }

    public static string Write(IReadOnlyList<BlockVm> blocks, MdTextMap map, string source, MdTextRange selection)
    {
        if (selection.IsEmpty)
        {
            return string.Empty;
        }

        var writer = new MdMarkdownWriter(map, source, selection);
        var lines = writer.Blocks(blocks);

        if (writer._texts is [var only] && !writer._other && (writer._cells.Contains(only) || !writer.Covers(only)))
        {
            return writer._codes.TryGetValue(only, out var code) ? writer.CodeFragment(code) : writer.Inlines(only);
        }

        return string.Join(
            "\n",
            lines
                .SkipWhile(line => line.Length == 0)
                .Reverse()
                .SkipWhile(line => line.Length == 0)
                .Reverse());
    }

    private static string CodeSpan(string text)
    {
        var fence = new string('`', LongestRun(text, '`') + 1);
        var pad = text.StartsWith('`') || text.EndsWith('`') ? " " : string.Empty;
        return fence + pad + text + pad + fence;
    }

    /// <summary>Самый длинный ряд знака подряд — длина ограждения кода берётся на единицу больше.</summary>
    internal static int LongestRun(string text, char symbol)
    {
        var longest = 0;
        var current = 0;
        foreach (var c in text)
        {
            current = c == symbol ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }

        return longest;
    }

    private static string Destination(string href)
        => href.IndexOfAny([' ', '(', ')', '<', '>']) >= 0
            ? $"<{href.Replace("<", "%3C").Replace(">", "%3E")}>"
            : href;

    private static string Escape(string text)
    {
        var escaped = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is '\\' or '`' or '*' or '_' or '[' or ']' or '<')
            {
                escaped.Append('\\');
            }

            escaped.Append(c);
        }

        return escaped.ToString();
    }

    /// <summary>Текст в начале строки не должен начинать блок: заголовок, цитату, пункт списка.</summary>
    private static string EscapeLineStart(string text)
        => text is [('#' or '>' or '+' or '-' or '=' or '|'), ..]
            ? "\\" + text
            : OrderedMarker().Replace(text, "$1\\$2");

    private static string Separator(TableVm table)
        => "|" + string.Concat(Enumerable
            .Range(0, table.ColumnCount)
            .Select(column => table.AlignmentOf(column) switch
            {
                TableAlignment.Left => " :--- |",
                TableAlignment.Center => " :---: |",
                TableAlignment.Right => " ---: |",
                _ => " --- |"
            }));

    private static List<Marker> Markers(RunVm run)
    {
        var markers = new List<Marker>();
        if ((run.Href ?? run.Hyperlink?.OriginalString) is { } href)
        {
            markers.Add(new("[", $"]({Destination(href)})", Emphasis: false));
        }

        if (run.Strike)
        {
            markers.Add(Strike);
        }

        if (run.Bold)
        {
            markers.Add(Bold);
        }

        if (run.Italic)
        {
            markers.Add(Italic);
        }

        if (run.Marked)
        {
            markers.Add(Marked);
        }

        if (run.Inserted)
        {
            markers.Add(Inserted);
        }

        if (run.Cite)
        {
            markers.Add(Cite);
        }

        if (run.Kbd)
        {
            markers.Add(new("<kbd>", "</kbd>", Emphasis: false));
        }

        if (run.Subscript)
        {
            markers.Add(new("<sub>", "</sub>", Emphasis: false));
        }

        if (run.Superscript)
        {
            markers.Add(new("<sup>", "</sup>", Emphasis: false));
        }

        return markers;
    }

    [GeneratedRegex(@"^(\d+)([.)])(?=\s|$)")]
    private static partial Regex OrderedMarker();

    private List<string> Blocks(IReadOnlyList<BlockVm> blocks)
    {
        var lines = new List<string>();
        foreach (var block in blocks)
        {
            if (_ranges.TryGetValue(block, out var range) && Touches(range))
            {
                lines.AddRange(Block(block, range));
            }
        }

        return lines;
    }

    private IEnumerable<string> Block(BlockVm block, MdTextRange range)
    {
        switch (block)
        {
            case BlankLineVm:
                _other = true;
                return [string.Empty];

            case ParagraphVm paragraph:
                return Inlines(paragraph.Inlines).Split('\n');

            case HeadingVm heading:
                return [$"{new string('#', heading.Level)} {Inlines(heading.Inlines).Replace("\\\n", " ").Replace('\n', ' ')}"];

            case CodeBlockVm code:
                return Code(code);

            case QuoteVm quote:
                return Blocks(quote.Blocks).Select(line => line.Length == 0 ? ">" : "> " + line);

            case ListVm list:
                return List(list);

            case TableVm table:
                return Table(table);

            case FigureVm figure:
                return Figure(figure);

            case FigureCaptionVm caption:
                return [Inlines(caption.Inlines)];

            case FooterVm footer:
                return Blocks(footer.Blocks).Select(line => line.Length == 0 ? "^^" : "^^ " + line);

            case CalloutVm callout:
                return Callout(callout);

            case DetailsVm details:
                return Details(details);

            case DefinitionListVm definitions:
                return Definitions(definitions);

            case AlignedVm aligned:
                return [$"<div align=\"{aligned.Alignment.ToString().ToLowerInvariant()}\">", string.Empty, .. Blocks(aligned.Blocks), string.Empty, "</div>"];

            case ThematicBreakVm:
                _other = true;
                return ["---"];

            case FootnoteVm footnote when _selection.Start <= range.Start && _selection.End >= range.End && SourceOf(footnote) is { } whole:
                _other = true;
                return whole.Split('\n');

            case FootnoteVm footnote:
                return Blocks(footnote.Blocks);

            case IMdTextSource widget:
                return Widget(block, widget, range);

            default:
                _other = true;
                return (SourceOf(block) ?? (block as IMdCopyText)?.CopyText ?? string.Empty).Split('\n');
        }
    }

    private List<string> Code(CodeBlockVm code)
    {
        if (!_map.TryGetRange(code.Runs, out var range) || Selected(code.Code, range) is not { Length: > 0 } text)
        {
            return [];
        }

        _texts.Add(code.Runs);
        _codes[code.Runs] = code;

        var fence = new string('`', Math.Max(3, LongestRun(text, '`') + 1));
        return [fence + (code.InfoString ?? string.Empty), .. text.Split('\n'), fence];
    }

    /// <summary>Кусок одного блока кода: несколько строк — блоком, кусок строки — кодом посреди строки.</summary>
    private string CodeFragment(CodeBlockVm code)
    {
        _map.TryGetRange(code.Runs, out var range);
        var text = Selected(code.Code, range);
        return text.Contains('\n') ? string.Join("\n", Code(code)) : CodeSpan(text);
    }

    private List<string> List(ListVm list)
    {
        var lines = new List<string>();
        foreach (var item in list.Items)
        {
            var content = Blocks(item.Blocks);
            if (content.Count == 0)
            {
                continue;
            }

            var marker = list.Ordered ? item.MarkerText : "-";
            var indent = new string(' ', marker.Length + 1);
            lines.Add($"{marker} {content[0]}");
            lines.AddRange(content.Skip(1).Select(line => line.Length == 0 ? line : indent + line));
        }

        return lines;
    }

    /// <summary>
    /// Таблица из задетых строк и, всегда, строки заголовка. Выделение внутри одной ячейки из
    /// блоков пишется её блоками.
    /// </summary>
    private List<string> Table(TableVm table)
    {
        foreach (var cell in table.Rows.SelectMany(row => row.Cells))
        {
            _cells.Add(cell.Inlines);
        }

        var inner = table.Rows
            .SelectMany(row => row.Cells)
            .FirstOrDefault(cell => cell.HasBlocks && CellRange(cell) is { } range && range.Start <= _selection.Start && _selection.End <= range.End);
        if (inner is not null)
        {
            return Blocks(inner.Blocks);
        }

        var touched = table.Rows
            .Where(row => row.Cells.Any(cell => CellRange(cell) is { } range && Touches(range)))
            .ToList();
        if (touched.Count == 0)
        {
            return [];
        }

        var header = table.Rows[0];
        var lines = new List<string>
        {
            Row(header, whole: !touched.Any(row => ReferenceEquals(row, header))),
            Separator(table)
        };
        lines.AddRange(touched.Where(row => !ReferenceEquals(row, header)).Select(row => Row(row, whole: false)));
        return lines;
    }

    private string Row(TableRowVm row, bool whole)
    {
        var cells = row.Cells
            .Select(cell => (cell.HasBlocks ? CellText(cell, whole) : Inlines(cell.Inlines, whole)).Replace("|", "\\|").Replace('\n', ' '))
            .Select(cell => cell.Length == 0 ? " " : $" {cell} ");
        return "|" + string.Join("|", cells) + "|";
    }

    /// <summary>Кусок карты ячейки: её строки текста или её блоков.</summary>
    private MdTextRange? CellRange(TableCellVm cell)
    {
        if (!cell.HasBlocks)
        {
            return _map.TryGetRange(cell.Inlines, out var range) ? range : null;
        }

        return _ranges.TryGetValue(cell.Blocks[0], out var first) && _ranges.TryGetValue(cell.Blocks[^1], out var last)
            ? MdTextRange.Between(first.Start, last.End)
            : null;
    }

    /// <summary>Ячейка из блоков строкой таблицы: её текст, блоки — через <c>&lt;br&gt;</c>.</summary>
    private string CellText(TableCellVm cell, bool whole)
    {
        if (CellRange(cell) is not { } range)
        {
            return string.Empty;
        }

        var text = _map.GetText(whole ? range : range.Intersect(_selection));
        return string.Join("<br>", text.Split('\n').Where(line => line.Length > 0).Select(Escape));
    }

    /// <summary>
    /// Плашка цитатой с видом в первой строке, как у GitHub; свой заголовок и сворачиваемость —
    /// после вида, как в Obsidian.
    /// </summary>
    private List<string> Callout(CalloutVm callout)
    {
        var fold = callout.IsFoldable ? callout.IsFolded ? "-" : "+" : string.Empty;
        var title = Inlines(callout.Title) is { Length: > 0 } text ? " " + text : string.Empty;
        var lines = new List<string> { $"> [!{callout.Kind.ToUpperInvariant()}]{fold}{title}" };
        lines.AddRange(Blocks(callout.Blocks).Select(line => line.Length == 0 ? ">" : "> " + line));
        return lines;
    }

    /// <summary>Сворачиваемый блок тегами <c>&lt;details&gt;</c> и <c>&lt;summary&gt;</c>, содержимое — markdown между ними.</summary>
    private List<string> Details(DetailsVm details)
    {
        var lines = new List<string> { details.IsOpen ? "<details open>" : "<details>" };
        if (Inlines(details.Summary) is { Length: > 0 } summary)
        {
            lines.Add($"<summary>{summary}</summary>");
        }

        lines.Add(string.Empty);
        lines.AddRange(Blocks(details.Blocks));
        lines.Add(string.Empty);
        lines.Add("</details>");
        return lines;
    }

    /// <summary>Термины — каждый своей строкой, определение — после <c>:</c> с отступом в четыре пробела.</summary>
    private List<string> Definitions(DefinitionListVm definitions)
    {
        var lines = new List<string>();
        foreach (var item in definitions.Items)
        {
            if (item.HasTerms && Inlines(item.Terms) is { Length: > 0 } terms)
            {
                lines.AddRange(terms.Split('\n'));
            }

            var content = Blocks(item.Blocks);
            if (content.Count == 0)
            {
                continue;
            }

            lines.Add(":   " + content[0]);
            lines.AddRange(content.Skip(1).Select(line => line.Length == 0 ? line : "    " + line));
        }

        return lines;
    }

    /// <summary>Фигура в ограждении <c>^^^</c>; подпись — на открывающей или закрывающей строке, где была.</summary>
    private List<string> Figure(FigureVm figure)
    {
        var opening = figure.Blocks is [FigureCaptionVm first, ..] ? first : null;
        var closing = figure.Blocks is [.., FigureCaptionVm last] && !ReferenceEquals(last, opening) ? last : null;
        var content = Blocks(figure.Blocks.Where(block => block is not FigureCaptionVm).ToList());

        return [Fence(opening), .. content, Fence(closing)];

        string Fence(FigureCaptionVm? caption)
            => caption is not null && Inlines(caption.Inlines) is { Length: > 0 } text ? "^^^ " + text : "^^^";
    }

    /// <summary>Виджет целиком — его исходником; кусок виджета — тем, что он отдаёт в карту.</summary>
    private string[] Widget(BlockVm block, IMdTextSource widget, MdTextRange range)
    {
        if (_selection.Start <= range.Start && _selection.End >= range.End && SourceOf(block) is { } whole)
        {
            _other = true;
            return whole.Split('\n');
        }

        var markdown = new StringBuilder();
        var position = range.Start;
        widget.WriteText(new(
            text =>
            {
                if (Selected(text, new(position, text.Length)) is { Length: > 0 } part)
                {
                    _other = true;
                    markdown.Append(new InlineMarkdown(this).Plain(part));
                }

                position += text.Length;
            },
            inlines =>
            {
                markdown.Append(Inlines(inlines));
                if (_map.TryGetRange(inlines, out var written))
                {
                    position = written.End;
                }
            },
            blocks =>
            {
                markdown.Append(string.Join("\n", Blocks(blocks)));
                if (blocks.Count > 0 && _ranges.TryGetValue(blocks[^1], out var last))
                {
                    position = last.End;
                }
            }));

        return markdown.ToString().Split('\n');
    }

    private string Inlines(IReadOnlyList<InlineVm> inlines, bool whole = false)
    {
        if (!_map.TryGetRange(inlines, out var range))
        {
            return string.Empty;
        }

        var markdown = new InlineMarkdown(this);
        var position = range.Start;
        foreach (var inline in inlines)
        {
            var length = MdTextMap.TextOf(inline).Length;
            var from = whole ? 0 : Math.Clamp(_selection.Start - position, 0, length);
            var to = whole ? length : Math.Clamp(_selection.End - position, 0, length);
            if (to > from)
            {
                markdown.Add(inline, from, to);
            }

            position += length;
        }

        if (!whole && !markdown.IsEmpty && !_texts.Contains(inlines))
        {
            _texts.Add(inlines);
        }

        return markdown.ToString();
    }

    private bool Touches(MdTextRange range)
        => range.IsEmpty
            ? range.Start > _selection.Start && range.Start < _selection.End
            : range.Start < _selection.End && range.End > _selection.Start;

    private bool Covers(IReadOnlyList<InlineVm> inlines)
        => _map.TryGetRange(inlines, out var range) && _selection.Start <= range.Start && _selection.End >= range.End;

    /// <summary>Часть текста, которая попала в выделение; <paramref name="range"/> — где текст в карте.</summary>
    private string Selected(string text, MdTextRange range)
    {
        var from = Math.Clamp(_selection.Start - range.Start, 0, text.Length);
        var to = Math.Clamp(_selection.End - range.Start, 0, text.Length);
        return to > from ? text[from..to] : string.Empty;
    }

    private string? SourceOf(NodeVm node)
        => node.Source is { IsEmpty: false } span && span.End <= _source.Length
            ? _source.Substring(span.Start, span.Length)
            : null;

    /// <summary>Начертание или ссылка: что ставится до куска текста и что после.</summary>
    private readonly record struct Marker(string Open, string Close, bool Emphasis);

    /// <summary>Строчная разметка: начертания и ссылки открываются и закрываются, когда меняются у кусков текста.</summary>
    private sealed class InlineMarkdown(MdMarkdownWriter writer)
    {
        private readonly StringBuilder _text = new();
        private readonly List<Marker> _open = [];
        private bool _lineStart = true;

        public bool IsEmpty => _text.Length == 0;

        public void Add(InlineVm inline, int from, int to)
        {
            switch (inline)
            {
                case RunVm { Code: false, Href: { } href } run when run.Text == href && to - from == run.Text.Length:
                    Open(Markers(run with { Href = null, Hyperlink = null }), href);
                    Append($"<{href}>");
                    break;

                case RunVm run:
                    var whole = to - from == run.Text.Length;
                    var text = Open(Markers(run), run.Text[from..to]);
                    Append(run.Code ? CodeSpan(text) : Plain(text, raw: whole && writer.SourceOf(run) == run.Text));
                    break;

                case LineBreakVm lineBreak:
                    Open([], string.Empty);
                    _text.Append(lineBreak.IsHard ? "\\\n" : "\n");
                    _lineStart = true;
                    break;

                case ImageVm image:
                    var wanted = _open.Where(marker => marker.Open != "[").ToList();
                    if ((image.Href ?? image.Hyperlink?.OriginalString) is { } link)
                    {
                        wanted.Add(new("[", $"]({Destination(link)})", Emphasis: false));
                    }

                    Append(Open(wanted, ImageMarkdown(image)));
                    break;

                default:
                    Append(writer.SourceOf(inline) ?? (inline as IMdCopyText)?.CopyText ?? string.Empty);
                    break;
            }
        }

        /// <summary>Текст без своего оформления.</summary>
        public string Plain(string text)
        {
            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    _text.Append('\n');
                    _lineStart = true;
                }

                Append(Plain(lines[i], raw: false));
            }

            return ToString();
        }

        public override string ToString()
        {
            Open([], string.Empty);
            return _text.ToString();
        }

        /// <summary>
        /// Закрывает лишнее и открывает недостающее перед куском текста; возвращает кусок без
        /// ведущих пробелов, если они ушли наружу открытых начертаний.
        /// </summary>
        private string Open(IReadOnlyList<Marker> wanted, string text)
        {
            var keep = 0;
            while (keep < _open.Count && wanted.Contains(_open[keep]))
            {
                keep++;
            }

            for (var i = _open.Count - 1; i >= keep; i--)
            {
                Close(_open[i]);
            }

            _open.RemoveRange(keep, _open.Count - keep);

            var opening = wanted.Where(marker => !_open.Contains(marker)).ToList();
            if (opening.Count == 0 || string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            var leading = text.Length - text.TrimStart().Length;
            Append(text[..leading]);
            foreach (var marker in opening)
            {
                _text.Append(marker.Open);
                _open.Add(marker);
            }

            _lineStart = false;
            return text[leading..];
        }

        private void Close(Marker marker)
        {
            if (!marker.Emphasis)
            {
                _text.Append(marker.Close);
                return;
            }

            // BYDESIGN: знак начертания ставится перед хвостовыми пробелами — после пробела
            // markdown не считает его закрывающим.
            var trailing = 0;
            while (trailing < _text.Length && char.IsWhiteSpace(_text[_text.Length - 1 - trailing]))
            {
                trailing++;
            }

            _text.Insert(_text.Length - trailing, marker.Close);
        }

        private void Append(string text)
        {
            _text.Append(text);
            _lineStart &= text.Length == 0;
        }

        /// <summary>Картинка своим исходником, если он markdown; картинка из HTML — <c>![alt](адрес "подсказка"){width=… height=…}</c>.</summary>
        private string ImageMarkdown(ImageVm image)
        {
            if (writer.SourceOf(image) is { } source && source.StartsWith("![", StringComparison.Ordinal))
            {
                return source;
            }

            var title = image.Title is { } text ? $" \"{text.Replace("\"", "\\\"")}\"" : string.Empty;
            var size = new[] { ("width", image.Width), ("height", image.Height) }
                .Where(pair => pair.Item2 is not null)
                .Select(pair => $"{pair.Item1}={pair.Item2!.Value.ToString(CultureInfo.InvariantCulture)}")
                .ToList();

            var attributes = size.Count > 0 ? "{" + string.Join(" ", size) + "}" : string.Empty;
            return $"![{Escape(image.Alt)}]({Destination(image.Url)}{title}){attributes}";
        }

        /// <summary>
        /// Текст как в исходнике, если он совпадает со своим исходником целиком; иначе —
        /// с обратной чертой перед знаками разметки.
        /// </summary>
        private string Plain(string text, bool raw)
        {
            var written = raw ? text : Escape(text);
            return _lineStart ? EscapeLineStart(written) : written;
        }
    }
}
