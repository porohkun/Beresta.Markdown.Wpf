namespace Beresta.Markdown.Wpf.Kit;

using System.Text.RegularExpressions;
using Extensions;
using Markdig.Extensions.CustomContainers;
using Markdig.Syntax;
using ViewModels;

/// <summary>
/// Containers <c>:::kind Title</c> of Docusaurus, VuePress and HackMD: a known kind is a callout,
/// <c>details</c> and <c>spoiler</c> are collapsible blocks. The title follows the name or stands
/// in square brackets.
/// </summary>
public sealed partial class CalloutContainers : MdExtension
{
    /// <inheritdoc/>
    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
    {
        if (block is not CustomContainer container)
        {
            return null;
        }

        var (line, heading) = CalloutKinds.FirstLine(container, context);
        if (Header().Match(line) is not { Success: true } header)
        {
            return null;
        }

        var name = header.Groups["name"].Value.ToLowerInvariant();
        var text = (header.Groups["bracket"].Success ? header.Groups["bracket"].Value : header.Groups["rest"].Value).Trim();
        IReadOnlyList<InlineVm> title = text.Length > 0 ? [new RunVm(text) { Source = heading }] : [];

        if (name is "details" or "spoiler")
        {
            return new DetailsVm(title, context.ConvertChildren(container))
            {
                IsOpen = header.Groups["options"].Value.Contains("open", StringComparison.OrdinalIgnoreCase)
            };
        }

        return CalloutKinds.Resolve(name) is not null ? CalloutKinds.Callout(name, title, context.ConvertChildren(container), heading) : null;
    }

    /// <inheritdoc/>
    public override MdSourceEdit? EditSource(NodeVm node, object? state, MdSourceContext context)
    {
        if (node is not DetailsVm details || state is not MdFoldState fold || details.Source.IsEmpty)
        {
            return null;
        }

        var text = context.Text;
        var start = details.Source.Start;
        var end = text.IndexOf('\n', start) is var newline and >= 0 ? newline : text.Length;
        if (end > start && text[end - 1] == '\r')
        {
            end--;
        }

        var header = Header().Match(text, start, end - start);
        var options = header.Groups["options"];
        if (!header.Success || (options.Success && options.Value.Contains("open", StringComparison.OrdinalIgnoreCase)) == fold.IsExpanded)
        {
            return null;
        }

        return fold.IsExpanded ? Opened(header) : Closed(text, start, options);
    }

    /// <summary>Разворачивание: <c>state="open"</c> в параметры заголовка, а без скобок — скобки после имени.</summary>
    private static MdSourceEdit Opened(Match header)
    {
        var options = header.Groups["options"];
        if (options.Success)
        {
            return new(options.Index + options.Length, 0, options.Value.Trim().Length == 0 ? "state=\"open\"" : " state=\"open\"");
        }

        var bracket = header.Groups["bracket"];
        var after = bracket.Success ? bracket.Index + bracket.Length + 1 : header.Groups["name"].Index + header.Groups["name"].Length;
        return new(after, 0, " {state=\"open\"}");
    }

    /// <summary>Сворачивание: из параметров уходит всё, где есть <c>open</c>; опустевшие скобки — с пробелами перед ними.</summary>
    private static MdSourceEdit Closed(string text, int start, Group options)
    {
        var kept = OptionToken().Matches(options.Value)
            .Select(token => token.Value)
            .Where(token => !token.Contains("open", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (kept.Count > 0)
        {
            return new(options.Index, options.Length, string.Join(' ', kept));
        }

        var from = text.LastIndexOf('{', options.Index);
        while (from > start && text[from - 1] is ' ' or '\t')
        {
            from--;
        }

        return new(from, options.Index + options.Length + 1 - from, string.Empty);
    }

    [GeneratedRegex(@"^\s*:{3,}\s*(?<name>[A-Za-z][\w-]*)(?:\[(?<bracket>[^\]]*)\])?(?:\s*\{(?<options>[^}]*)\})?\s*(?<rest>.*?)\s*$")]
    private static partial Regex Header();

    [GeneratedRegex("""[^\s=]+(?:=(?:"[^"]*"|'[^']*'|\S+))?""")]
    private static partial Regex OptionToken();
}
