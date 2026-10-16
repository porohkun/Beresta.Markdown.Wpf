namespace MarkdigWpfViewer.Kit;

using Extensions;
using Markdig.Syntax;
using ViewModels;

/// <summary>Виды плашек готовых расширений: виды со своими ключами темы и их другие имена из Obsidian и MkDocs.</summary>
internal static class CalloutKinds
{
    private static readonly HashSet<string> Known =
    [
        "note", "tip", "important", "warning", "caution", "abstract", "info", "todo", "success", "question", "failure", "danger", "bug", "example", "quote"
    ];

    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["summary"] = "abstract",
        ["tldr"] = "abstract",
        ["hint"] = "tip",
        ["check"] = "success",
        ["done"] = "success",
        ["help"] = "question",
        ["faq"] = "question",
        ["attention"] = "warning",
        ["fail"] = "failure",
        ["missing"] = "failure",
        ["error"] = "danger",
        ["cite"] = "quote"
    };

    /// <summary>Вид по имени из разметки: сам вид или тот, чьё это другое имя; незнакомое — <c>null</c>.</summary>
    public static string? Resolve(string name)
    {
        var lower = name.Trim().ToLowerInvariant();
        return Known.Contains(lower) ? lower : Aliases.GetValueOrDefault(lower);
    }

    /// <summary>
    /// Плашка вида <paramref name="name"/>: другое имя рисуется своим видом с этим именем в заголовке
    /// на строке <paramref name="heading"/>, незнакомое — как заметка.
    /// </summary>
    public static CalloutVm Callout(string name, IReadOnlyList<InlineVm> title, IReadOnlyList<BlockVm> blocks, MdSourceSpan heading)
    {
        var lower = name.Trim().ToLowerInvariant();
        var kind = Resolve(lower) ?? lower;
        if (title.Count == 0 && kind != lower)
        {
            title = [new RunVm(char.ToUpperInvariant(lower[0]) + lower[1..]) { Source = heading }];
        }

        return new(kind, title, blocks);
    }

    /// <summary>Первая строка блока: её текст и место в исходнике.</summary>
    public static (string Text, MdSourceSpan Span) FirstLine(Block block, MdConvertContext context)
    {
        var source = context.GetSource(block);
        var end = source.IndexOf('\n');
        var text = (end < 0 ? source : source[..end]).TrimEnd('\r');
        return (text, new(block.Span.Start, text.Length, block.Line, block.Line));
    }
}
