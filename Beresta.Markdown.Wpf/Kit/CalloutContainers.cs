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

    [GeneratedRegex(@"^\s*:{3,}\s*(?<name>[A-Za-z][\w-]*)(?:\[(?<bracket>[^\]]*)\])?(?:\s*\{(?<options>[^}]*)\})?\s*(?<rest>.*?)\s*$")]
    private static partial Regex Header();
}
