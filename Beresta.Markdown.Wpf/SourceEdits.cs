namespace Beresta.Markdown.Wpf;

using System.Text.RegularExpressions;
using ViewModels;

/// <summary>Правки исходника от встроенных контролов: флажка задачи и HTML-тега <c>&lt;details&gt;</c>.</summary>
internal static partial class SourceEdits
{
    /// <summary>Знак между скобками задачи: отмеченная — пробел, пустая — <c>x</c>.</summary>
    public static MdSourceEdit? ToggleTask(TaskVm task, string source)
    {
        var mark = task.Source.Start + 1;
        if (task.Source.Length < 3 || mark >= source.Length || source[task.Source.Start] != '[')
        {
            return null;
        }

        return new(mark, 1, source[mark] is 'x' or 'X' ? " " : "x");
    }

    /// <summary>Атрибут <c>open</c> в открывающем теге <c>&lt;details&gt;</c>: добавить или убрать в любом написании.</summary>
    public static MdSourceEdit? SetDetailsOpen(DetailsVm details, bool open, string source)
    {
        if (details.Source.IsEmpty || OpeningTag().Match(source, details.Source.Start, details.Source.Length) is not { Success: true } tag)
        {
            return null;
        }

        var attribute = Attribute().Matches(tag.Groups["attributes"].Value)
            .FirstOrDefault(match => string.Equals(match.Groups["name"].Value, "open", StringComparison.OrdinalIgnoreCase));
        if (open == attribute is not null)
        {
            return null;
        }

        var attributes = tag.Groups["attributes"].Index;
        return attribute is null
            ? new(attributes + tag.Groups["attributes"].Length, 0, " open")
            : new(attributes + attribute.Index, attribute.Length, string.Empty);
    }

    [GeneratedRegex(@"\G[ \t]*<details(?<attributes>(?:\s[^>]*?)?)\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex OpeningTag();

    [GeneratedRegex("""\s+(?<name>[a-zA-Z_:][-a-zA-Z0-9_:.]*)(?:\s*=\s*(?:"[^"]*"|'[^']*'|[^\s"'=<>`]+))?""")]
    private static partial Regex Attribute();
}
