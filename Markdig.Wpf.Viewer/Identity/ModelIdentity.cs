namespace MarkdigWpfViewer.Identity;

using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Память вьюера между разборами. Узел нового разбора получает идентификатор элемента прошлого
/// разбора — с тем же текстом или изменённого на своём месте; вьюмодель, равная прошлой по
/// содержимому, берётся прошлая.
/// </summary>
internal sealed class ModelIdentity
{
    /// <summary>Больше клеток сравнение двух последовательностей не тратит: сверх — только по местам.</summary>
    private const long MaxDiffCells = 250_000;

    private static readonly Entry[] NoEntries = [];

    private readonly Dictionary<MarkdownObject, long> _ids = new(ReferenceEqualityComparer.Instance);

    private long _lastId;
    private Entry[] _roots = NoEntries;
    private Dictionary<long, Entry[]> _children = [];
    private Dictionary<(long Previous, int Index), long> _blankLines = [];
    private Dictionary<long, NodeVm> _models = [];

    private Entry[] _nextRoots = NoEntries;
    private Dictionary<long, Entry[]> _nextChildren = [];
    private Dictionary<(long Previous, int Index), long> _nextBlankLines = [];
    private Dictionary<long, NodeVm> _nextModels = [];

    /// <summary>Сопоставляет узлы нового разбора с прошлым; вызывается до превращения в модель.</summary>
    public void Match(MarkdownDocument document, string source)
    {
        _ids.Clear();
        _nextChildren = [];
        _nextBlankLines = [];
        _nextModels = [];
        _nextRoots = MatchSequence(ChildrenOf(document), _roots, source);
    }

    /// <summary>Делает новый разбор прошлым для следующего.</summary>
    public void Commit()
    {
        _roots = _nextRoots;
        _children = _nextChildren;
        _blankLines = _nextBlankLines;
        _models = _nextModels;
        _ids.Clear();
    }

    /// <summary>Идентификатор узла нового разбора; 0 — узел не из сопоставленного документа.</summary>
    public long IdOf(MarkdownObject node) => _ids.TryGetValue(node, out var id) ? id : 0;

    /// <summary>Идентификатор отступа — <paramref name="index"/>-го подряд после элемента <paramref name="previous"/>.</summary>
    public long BlankLineId(long previous, int index)
    {
        var key = (previous, index);
        if (previous == 0 || _nextBlankLines.ContainsKey(key))
        {
            return ++_lastId;
        }

        var id = _blankLines.TryGetValue(key, out var known) ? known : ++_lastId;
        _nextBlankLines[key] = id;
        return id;
    }

    /// <summary>Прошлая вьюмодель с тем же идентификатором, если новая равна ей по содержимому; иначе новая.</summary>
    public T Reuse<T>(T model)
        where T : NodeVm
    {
        if (model.NodeId == 0)
        {
            return model;
        }

        if (_models.TryGetValue(model.NodeId, out var old) && old is T same && ModelComparer.Equal(same, model))
        {
            model = same;
        }

        _nextModels[model.NodeId] = model;
        return model;
    }

    private Entry[] MatchSequence(IReadOnlyList<MarkdownObject> nodes, Entry[] old, string source)
    {
        if (nodes.Count == 0)
        {
            return NoEntries;
        }

        var keys = new Key[nodes.Count];
        for (var i = 0; i < nodes.Count; i++)
        {
            keys[i] = KeyOf(nodes[i], source);
        }

        var matched = Align(old, keys);
        var entries = new Entry[nodes.Count];

        for (var i = 0; i < nodes.Count; i++)
        {
            var id = matched[i] != 0 ? matched[i] : ++_lastId;
            _ids[nodes[i]] = id;
            entries[i] = new Entry(keys[i], id);

            var children = ChildrenOf(nodes[i]);
            if (children.Count > 0)
            {
                var oldChildren = matched[i] != 0 && _children.TryGetValue(matched[i], out var known) ? known : NoEntries;
                _nextChildren[id] = MatchSequence(children, oldChildren, source);
            }
        }

        return entries;
    }

    /// <summary>
    /// Для каждого нового узла — идентификатор прошлого: общие начало и конец, затем наибольшая
    /// общая подпоследовательность, а между совпавшими — изменённые на месте узлы того же вида.
    /// </summary>
    private static long[] Align(Entry[] old, Key[] keys)
    {
        var matched = new long[keys.Length];
        var oldEnd = old.Length;
        var newEnd = keys.Length;

        var start = 0;
        while (start < oldEnd && start < newEnd && old[start].Key == keys[start])
        {
            matched[start] = old[start].Id;
            start++;
        }

        while (oldEnd > start && newEnd > start && old[oldEnd - 1].Key == keys[newEnd - 1])
        {
            matched[newEnd - 1] = old[oldEnd - 1].Id;
            oldEnd--;
            newEnd--;
        }

        var pairs = (long)(oldEnd - start) * (newEnd - start) <= MaxDiffCells
            ? CommonSubsequence(old, keys, start, oldEnd, newEnd)
            : [];

        var oldNext = start;
        var newNext = start;
        foreach (var (oldIndex, newIndex) in pairs)
        {
            PairInPlace(old, keys, matched, oldNext, oldIndex, newNext, newIndex);
            matched[newIndex] = old[oldIndex].Id;
            oldNext = oldIndex + 1;
            newNext = newIndex + 1;
        }

        PairInPlace(old, keys, matched, oldNext, oldEnd, newNext, newEnd);
        return matched;
    }

    private static List<(int Old, int New)> CommonSubsequence(Entry[] old, Key[] keys, int start, int oldEnd, int newEnd)
    {
        var pairs = new List<(int Old, int New)>();
        var rows = oldEnd - start;
        var columns = newEnd - start;
        if (rows == 0 || columns == 0)
        {
            return pairs;
        }

        var lengths = new int[rows + 1, columns + 1];
        for (var i = rows - 1; i >= 0; i--)
        {
            for (var j = columns - 1; j >= 0; j--)
            {
                lengths[i, j] = old[start + i].Key == keys[start + j]
                    ? lengths[i + 1, j + 1] + 1
                    : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
            }
        }

        for (int i = 0, j = 0; i < rows && j < columns;)
        {
            if (old[start + i].Key == keys[start + j])
            {
                pairs.Add((start + i, start + j));
                i++;
                j++;
            }
            else if (lengths[i + 1, j] >= lengths[i, j + 1])
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return pairs;
    }

    private static void PairInPlace(Entry[] old, Key[] keys, long[] matched, int oldStart, int oldEnd, int newStart, int newEnd)
    {
        for (int o = oldStart, n = newStart; o < oldEnd && n < newEnd && old[o].Key.Kind == keys[n].Kind; o++, n++)
        {
            matched[n] = old[o].Id;
        }
    }

    private static IReadOnlyList<MarkdownObject> ChildrenOf(MarkdownObject node)
    {
        return node switch
        {
            ContainerBlock container => [.. container],
            LeafBlock { Inline: { } inline } => InlinesOf(inline),
            ContainerInline container => InlinesOf(container),
            _ => []
        };
    }

    private static List<MarkdownObject> InlinesOf(ContainerInline container)
    {
        var inlines = new List<MarkdownObject>();
        for (var child = container.FirstChild; child is not null; child = child.NextSibling)
        {
            inlines.Add(child);
        }

        return inlines;
    }

    private static Key KeyOf(MarkdownObject node, string source)
    {
        var span = node.Span;
        return span.IsEmpty || span.Start < 0 || span.End >= source.Length
            ? new Key(node.GetType(), 0, 0)
            : new Key(node.GetType(), span.Length, string.GetHashCode(source.AsSpan(span.Start, span.Length)));
    }

    /// <summary>Вид узла и его текст в исходнике — длиной и свёрткой.</summary>
    private readonly record struct Key(Type Kind, int Length, int Hash);

    private readonly record struct Entry(Key Key, long Id);
}
