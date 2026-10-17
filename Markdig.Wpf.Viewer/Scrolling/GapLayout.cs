namespace MarkdigWpfViewer.Scrolling;

using System.Windows;
using Selection;
using ViewModels;

/// <summary>
/// Где во вьюере встают пустые места: перед самым внешним элементом, который начинается с их
/// строки, — блоком, пунктом списка, строкой таблицы, строкой текста или кода. С такой строки
/// ничего не начинается — место встаёт на ближайшую границу блока.
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
            Place(blocks, gap.Line, gap.Height, []);
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

    /// <summary>Ставит место среди соседей: перед первым, кто начинается с его строки или ниже, а внутри соседа — глубже.</summary>
    private bool Place(IEnumerable<NodeVm> siblings, int line, double height, List<NodeVm> path)
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
                Add(_before, node, height, path);
                return true;
            }

            if (line > source.EndLine)
            {
                continue;
            }

            path.Add(node);
            var placed = PlaceInside(node, line, height, path);
            path.RemoveAt(path.Count - 1);
            if (!placed)
            {
                Add(line - source.StartLine <= source.EndLine + 1 - line ? _before : _after, node, height, path);
            }

            return true;
        }

        if (last is null)
        {
            return false;
        }

        Add(_after, last, height, path);
        return true;
    }

    private bool PlaceInside(NodeVm node, int line, double height, List<NodeVm> path)
    {
        return node switch
        {
            ParagraphVm paragraph => PlaceInText(paragraph.Inlines, line, height, path),
            HeadingVm heading => PlaceInText(heading.Inlines, line, height, path),
            CodeBlockVm code => PlaceInCode(code, line, height, path),
            TableVm table => PlaceInTable(table, line, height, path),
            ListVm list => Place(list.Items, line, height, path),
            ListItemVm item => Place(item.Blocks, line, height, path),
            QuoteVm quote => Place(quote.Blocks, line, height, path),
            CalloutVm callout => Place(callout.Blocks, line, height, path),
            DetailsVm details => Place(details.Blocks, line, height, path),
            FigureVm figure => Place(figure.Blocks, line, height, path),
            FooterVm footer => Place(footer.Blocks, line, height, path),
            AlignedVm aligned => Place(aligned.Blocks, line, height, path),
            FootnoteVm footnote => Place(footnote.Blocks, line, height, path),
            _ => false
        };
    }

    /// <summary>Перед экранной строкой текста, которая начинается со строки исходника после переноса.</summary>
    private bool PlaceInText(IReadOnlyList<InlineVm> inlines, int line, double height, List<NodeVm> path)
    {
        var offset = 0;
        for (var i = 0; i < inlines.Count; i++)
        {
            if (i > 0 && inlines[i - 1] is LineBreakVm && inlines[i].Source is { IsEmpty: false, StartLine: var start } && start == line)
            {
                AddText(inlines, offset, height, path);
                return true;
            }

            offset += MdTextMap.TextOf(inlines[i]).Length;
        }

        return false;
    }

    /// <summary>Перед строкой кода; перед первой — между шапкой и кодом.</summary>
    private bool PlaceInCode(CodeBlockVm code, int line, double height, List<NodeVm> path)
    {
        var lines = code.Code.Split('\n');
        var index = line - code.CodeFirstLine;
        if (index < 0 || index >= lines.Length)
        {
            return false;
        }

        var offset = lines.Take(index).Sum(text => text.Length + 1);
        AddText(code.Runs, offset, height, path);

        if (!_code.TryGetValue(code, out var gaps))
        {
            _code[code] = gaps = [];
        }

        gaps.Add((index, height));
        return true;
    }

    /// <summary>Перед строкой таблицы, кроме первой: перед первой место встаёт перед таблицей.</summary>
    private bool PlaceInTable(TableVm table, int line, double height, List<NodeVm> path)
    {
        var row = table.Rows.Skip(1).FirstOrDefault(candidate => candidate.Source is { IsEmpty: false } source && source.StartLine == line);
        if (row is null)
        {
            return false;
        }

        Add(_before, row, height, path);
        return true;
    }

    private void AddText(object inlines, int offset, double height, List<NodeVm> path)
    {
        if (!_text.TryGetValue(inlines, out var gaps))
        {
            _text[inlines] = gaps = [];
        }

        gaps.Add((offset, height));
        AddInside(path, height);
    }

    private void Add(Dictionary<object, double> places, object node, double height, List<NodeVm> path)
    {
        places[node] = places.GetValueOrDefault(node) + height;
        AddInside(path, height);
    }

    private void AddInside(List<NodeVm> path, double height)
    {
        foreach (var ancestor in path)
        {
            _inside[ancestor] = _inside.GetValueOrDefault(ancestor) + height;
        }
    }
}
