namespace Beresta.Markdown.Wpf;

using ViewModels;

/// <summary>
/// Правимые единицы документа: наверху — блоки; в цитате, плашке и сворачиваемом блоке — их блоки, в
/// списке — пункты, в пункте — его блоки после первого. Внутрь свёрнутого не заходит.
/// </summary>
internal static class SourceUnits
{
    /// <summary>Уровни единиц вокруг строки, от верхнего до самого глубокого.</summary>
    public static List<MdSourceLevel> LevelsAt(
        IReadOnlyList<BlockVm> blocks,
        int line,
        MdSourceSpan whole,
        IReadOnlyList<int> lineStarts,
        Func<NodeVm, bool> folded)
    {
        IReadOnlyList<NodeVm> units = blocks
            .Where(block => !block.Source.IsEmpty)
            .ToList();
        var levels = new List<MdSourceLevel> { new(whole, Spans(units), 0) };

        while (units.FirstOrDefault(unit => unit.Source.StartLine <= line && line <= unit.Source.EndLine) is { } unit
               && ChildrenOf(unit, folded) is { Count: > 0 } children
               && line >= FirstChildLine(unit, children)
               && line <= children[^1].Source.EndLine)
        {
            var spans = Spans(children);
            spans[0] = Clip(spans[0], FirstChildLine(unit, children), lineStarts);
            levels.Add(new(unit.Source, spans, IndentOf(unit, lineStarts)));
            units = children;
        }

        return levels;
    }

    /// <summary>
    /// Блоки с окном: окно встаёт на самый глубокий уровень, где его строки укладываются в единицы
    /// одного контейнера, не задевая строк самого контейнера. <paramref name="path"/> получает
    /// идентификаторы контейнеров, внутри которых оно встало.
    /// </summary>
    public static IReadOnlyList<BlockVm> Place(
        IReadOnlyList<BlockVm> blocks,
        SourceWindowVm window,
        Func<NodeVm, bool> folded,
        ISet<long> path)
    {
        var touched = blocks
            .Where(block => Touches(block, window))
            .ToList();
        if (touched.Count == 1 && Inside(touched[0], window, folded, path) is { } inside)
        {
            return blocks
                .SelectMany(block => ReferenceEquals(block, touched[0]) ? inside : [block])
                .ToList();
        }

        return Replace(blocks, window);
    }

    /// <summary>Дети единицы, которые сами единицы; у листа и свёрнутого — <c>null</c>.</summary>
    private static List<NodeVm>? ChildrenOf(NodeVm node, Func<NodeVm, bool> folded)
    {
        IEnumerable<NodeVm>? children = node switch
        {
            QuoteVm quote => quote.Blocks,
            CalloutVm callout when !folded(callout) => callout.Blocks,
            DetailsVm details when !folded(details) => details.Blocks,
            ListVm list => list.Items,
            ListItemVm item => item.Blocks.Skip(1),
            _ => null
        };

        return children?
            .Where(child => !child.Source.IsEmpty)
            .ToList();
    }

    /// <summary>
    /// Контейнер с окном внутри — одним или, если это делённый список, несколькими блоками; окно в
    /// единицы контейнера не укладывается — <c>null</c>.
    /// </summary>
    private static List<BlockVm>? Inside(BlockVm container, SourceWindowVm window, Func<NodeVm, bool> folded, ISet<long> path)
    {
        if (!Fits(container, window, folded))
        {
            return null;
        }

        switch (container)
        {
            case QuoteVm quote:
                path.Add(quote.NodeId);
                return [quote with { Blocks = Place(quote.Blocks, window, folded, path) }];

            case CalloutVm callout:
                path.Add(callout.NodeId);
                return [callout with { Blocks = Place(callout.Blocks, window, folded, path) }];

            case DetailsVm details:
                path.Add(details.NodeId);
                return [details with { Blocks = Place(details.Blocks, window, folded, path) }];

            case ListVm list:
                return InList(list, window, folded, path);

            default:
                return null;
        }
    }

    /// <summary>
    /// Список с окном: окно внутри одного пункта — в его блоках после первого; на пунктах целиком —
    /// список делится на пункты до окна и пункты после.
    /// </summary>
    private static List<BlockVm> InList(ListVm list, SourceWindowVm window, Func<NodeVm, bool> folded, ISet<long> path)
    {
        var first = -1;
        var last = -1;
        for (var i = 0; i < list.Items.Count; i++)
        {
            if (Touches(list.Items[i], window))
            {
                first = first < 0 ? i : first;
                last = i;
            }
        }

        if (first >= 0 && first == last && Fits(list.Items[first], window, folded))
        {
            var item = list.Items[first];
            path.Add(list.NodeId);
            path.Add(item.NodeId);
            var placed = item with { Blocks = [item.Blocks[0], .. Place([.. item.Blocks.Skip(1)], window, folded, path)] };
            return [list with { Items = [.. list.Items.Select(other => ReferenceEquals(other, item) ? placed : other)] }];
        }

        var before = list.Items.Take(Math.Max(first, 0)).ToList();
        var after = list.Items.Skip(last + 1).ToList();
        var parts = new List<BlockVm>();
        if (before.Count > 0)
        {
            parts.Add(list with { Items = before });
        }

        parts.Add(window);
        if (after.Count > 0)
        {
            parts.Add(list with { Items = after, NodeId = before.Count > 0 ? SourceWindowVm.AfterListId : list.NodeId });
        }

        return parts;
    }

    /// <summary>Блоки с окном вместо тех, что задевают его строки; окно встаёт на место первого из них.</summary>
    private static List<BlockVm> Replace(IReadOnlyList<BlockVm> blocks, SourceWindowVm window)
    {
        var (first, last) = (window.Source.StartLine, window.Source.EndLine);
        var shown = new List<BlockVm>(blocks.Count + 1);
        var placed = false;
        foreach (var block in blocks)
        {
            var source = block.Source;
            var below = !source.IsEmpty && source.StartLine > last;
            var under = !source.IsEmpty && !below && source.EndLine >= first;
            if ((under || below) && !placed)
            {
                shown.Add(window);
                placed = true;
            }

            if (!under)
            {
                shown.Add(block);
            }
        }

        if (!placed)
        {
            shown.Add(window);
        }

        return shown;
    }

    /// <summary>Строки окна лежат на единицах контейнера, не задевая строк самого контейнера.</summary>
    private static bool Fits(NodeVm container, SourceWindowVm window, Func<NodeVm, bool> folded)
        => ChildrenOf(container, folded) is { Count: > 0 } children
           && window.Source.StartLine >= FirstChildLine(container, children)
           && window.Source.EndLine <= children[^1].Source.EndLine;

    /// <summary>
    /// Первая строка единиц контейнера: у плашки и сворачиваемого блока первая строка — их собственная,
    /// даже если на ней начинается первый ребёнок, как абзац в оповещении <c>&gt; [!NOTE]</c>.
    /// </summary>
    private static int FirstChildLine(NodeVm container, List<NodeVm> children)
        => container is CalloutVm or DetailsVm
            ? Math.Max(children[0].Source.StartLine, container.Source.StartLine + 1)
            : children[0].Source.StartLine;

    /// <summary>Место единицы со строки <paramref name="line"/>, если оно начинается выше.</summary>
    private static MdSourceSpan Clip(MdSourceSpan span, int line, IReadOnlyList<int> lineStarts)
        => span.StartLine >= line || line > span.EndLine || line >= lineStarts.Count
            ? span
            : new(lineStarts[line], span.End - lineStarts[line], line, span.EndLine);

    private static bool Touches(NodeVm node, SourceWindowVm window)
        => !node.Source.IsEmpty && node.Source.StartLine <= window.Source.EndLine && node.Source.EndLine >= window.Source.StartLine;

    /// <summary>Сколько знаков в начале строк своих единиц контейнер рисует по-своему: у пункта — отступ его текста.</summary>
    private static int IndentOf(NodeVm container, IReadOnlyList<int> lineStarts)
        => container is ListItemVm { Blocks: [{ Source: { IsEmpty: false } first }, ..] } && first.StartLine < lineStarts.Count
            ? first.Start - lineStarts[first.StartLine]
            : 0;

    private static List<MdSourceSpan> Spans(IEnumerable<NodeVm> units)
        => units
            .Select(unit => unit.Source)
            .ToList();
}
