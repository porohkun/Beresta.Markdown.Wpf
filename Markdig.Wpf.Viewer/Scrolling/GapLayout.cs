namespace MarkdigWpfViewer.Scrolling;

using System.Windows;
using Selection;
using ViewModels;

/// <summary>
/// Где во вьюере встают пустые места: перед самым внешним элементом, который начинается с их
/// строки, — блоком, пунктом списка, строкой таблицы, строкой текста или кода, закрывающей оградой
/// кода. С такой строки ничего не начинается — место встаёт на ближайшую границу блока.
/// </summary>
internal sealed class GapLayout
{
    /// <summary>Раскладка пустых мест документа; ставит вьюер, наследуют его презентеры.</summary>
    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.RegisterAttached(
            "Layout",
            typeof(GapLayout),
            typeof(GapLayout),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    private readonly Dictionary<object, double> _before = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, double> _after = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, double> _inside = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, List<(int Offset, double Height)>> _text = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, List<(int Index, double Height)>> _code = new(ReferenceEqualityComparer.Instance);

    public GapLayout(IReadOnlyList<BlockVm> blocks, IReadOnlyList<MdGap> gaps)
    {
        Gaps = gaps
            .Where(gap => gap.Height > 0)
            .GroupBy(gap => gap.Line)
            .Select(group => new MdGap(group.Key, group.Sum(gap => gap.Height)))
            .OrderBy(gap => gap.Line)
            .ToList();

        foreach (var gap in Gaps)
        {
            var path = new List<NodeVm>();
            if (Locate(blocks, gap.Line, path) is { } point)
            {
                Add(point, gap.Height, path);
            }
        }
    }

    public static GapLayout Empty { get; } = new([], []);

    /// <summary>Пустые места по строкам, без нулевых, сложенные по строке.</summary>
    public IReadOnlyList<MdGap> Gaps { get; }

    /// <summary>Место перед элементом в его списке — блоком или пунктом.</summary>
    public double Before(object item) => _before.GetValueOrDefault(item);

    /// <summary>Место после элемента в его списке — у ближайшей границы за ним.</summary>
    public double After(object item) => _after.GetValueOrDefault(item);

    /// <summary>Сколько мест встало внутри элемента — в оценку высоты несозданного блока.</summary>
    public double Inside(object item) => _inside.GetValueOrDefault(item);

    /// <summary>Места внутри текста: смещение экранной строки в тексте списка инлайнов и высота.</summary>
    public IReadOnlyList<(int Offset, double Height)> InText(object inlines)
        => _text.TryGetValue(inlines, out var gaps) ? gaps : [];

    /// <summary>Места перед строками кода: номер строки кода с нуля и высота.</summary>
    public IReadOnlyList<(int Index, double Height)> InCode(CodeBlockVm code)
        => _code.TryGetValue(code, out var gaps) ? gaps : [];

    /// <summary>
    /// Куда встанет пустое место строки среди блоков, начиная с блока <paramref name="from"/>;
    /// <paramref name="path"/> получает предков места сверху вниз. Блоков со строками нет — <c>null</c>.
    /// </summary>
    public static GapPoint? Locate(IReadOnlyList<BlockVm> blocks, int line, List<NodeVm> path, int from = 0)
        => LocateAmong(blocks.Skip(from), line, path);

    /// <summary>Место среди соседей: перед первым, кто начинается с его строки или ниже, а внутри соседа — глубже.</summary>
    private static GapPoint? LocateAmong(IEnumerable<NodeVm> siblings, int line, List<NodeVm> path)
    {
        NodeVm? last = null;
        foreach (var node in siblings)
        {
            var source = node.Source;
            if (source.IsEmpty)
            {
                continue;
            }

            last = node;
            if (source.StartLine >= line)
            {
                return new(GapPlace.Before, node, 0);
            }

            if (line > source.EndLine)
            {
                continue;
            }

            path.Add(node);
            if (LocateInside(node, line, path) is { } inside)
            {
                return inside;
            }

            path.RemoveAt(path.Count - 1);
            return new(line - source.StartLine <= source.EndLine + 1 - line ? GapPlace.Before : GapPlace.After, node, 0);
        }

        return last is null ? null : new GapPoint(GapPlace.After, last, 0);
    }

    private static GapPoint? LocateInside(NodeVm node, int line, List<NodeVm> path)
    {
        return node switch
        {
            ParagraphVm paragraph => LocateInText(paragraph, paragraph.Inlines, line),
            HeadingVm heading => LocateInText(heading, heading.Inlines, line),
            CodeBlockVm code => LocateInCode(code, line),
            TableVm table => LocateInTable(table, line),
            ListVm list => LocateAmong(list.Items, line, path),
            ListItemVm item => LocateAmong(item.Blocks, line, path),
            QuoteVm quote => LocateAmong(quote.Blocks, line, path),
            CalloutVm callout => LocateAmong(callout.Blocks, line, path),
            DetailsVm details => LocateAmong(details.Blocks, line, path),
            FigureVm figure => LocateAmong(figure.Blocks, line, path),
            FooterVm footer => LocateAmong(footer.Blocks, line, path),
            AlignedVm aligned => LocateAmong(aligned.Blocks, line, path),
            FootnoteVm footnote => LocateAmong(footnote.Blocks, line, path),
            _ => null
        };
    }

    /// <summary>Перед экранной строкой текста, которая начинается со строки исходника после переноса.</summary>
    private static GapPoint? LocateInText(NodeVm node, IReadOnlyList<InlineVm> inlines, int line)
    {
        var offset = 0;
        for (var i = 0; i < inlines.Count; i++)
        {
            if (i > 0 && inlines[i - 1] is LineBreakVm && inlines[i].Source is { IsEmpty: false, StartLine: var start } && start == line)
            {
                return new(GapPlace.Row, node, offset);
            }

            offset += MdTextMap.TextOf(inlines[i]).Length;
        }

        return null;
    }

    /// <summary>Перед строкой кода; перед первой — между шапкой и кодом, перед закрывающей оградой — под последней.</summary>
    private static GapPoint? LocateInCode(CodeBlockVm code, int line)
    {
        var lines = code.Code.Split('\n');
        var index = line - code.CodeFirstLine;
        var fence = code.IsFenced && index == lines.Length && line == code.Source.EndLine;
        return index < 0 || (index >= lines.Length && !fence)
            ? null
            : new GapPoint(GapPlace.Row, code, lines.Take(index).Sum(text => text.Length + 1));
    }

    /// <summary>Перед строкой таблицы, кроме первой: перед первой место встаёт перед таблицей.</summary>
    private static GapPoint? LocateInTable(TableVm table, int line)
    {
        var row = table.Rows.Skip(1).FirstOrDefault(candidate => candidate.Source is { IsEmpty: false } source && source.StartLine == line);
        return row is null ? null : new GapPoint(GapPlace.Before, row, 0);
    }

    private void Add(GapPoint point, double height, List<NodeVm> path)
    {
        switch (point)
        {
            case { Place: GapPlace.Before }:
                _before[point.Node] = _before.GetValueOrDefault(point.Node) + height;
                break;
            case { Place: GapPlace.After }:
                _after[point.Node] = _after.GetValueOrDefault(point.Node) + height;
                break;
            case { Node: CodeBlockVm code }:
                AddText(code.Runs, point.Offset, height);
                AddCode(code, CodeLineAt(code, point.Offset), height);
                break;
            case { Node: ParagraphVm paragraph }:
                AddText(paragraph.Inlines, point.Offset, height);
                break;
            case { Node: HeadingVm heading }:
                AddText(heading.Inlines, point.Offset, height);
                break;
        }

        foreach (var ancestor in path)
        {
            _inside[ancestor] = _inside.GetValueOrDefault(ancestor) + height;
        }
    }

    /// <summary>Номер строки кода с нуля, которая начинается со смещения; за концом кода — следующий за последней.</summary>
    private static int CodeLineAt(CodeBlockVm code, int offset)
        => offset > code.Code.Length
            ? code.Code.Split('\n').Length
            : code.Code.AsSpan(0, offset).Count('\n');

    private void AddText(object inlines, int offset, double height)
    {
        if (!_text.TryGetValue(inlines, out var gaps))
        {
            _text[inlines] = gaps = [];
        }

        gaps.Add((offset, height));
    }

    private void AddCode(CodeBlockVm code, int index, double height)
    {
        if (!_code.TryGetValue(code, out var gaps))
        {
            _code[code] = gaps = [];
        }

        gaps.Add((index, height));
    }
}
