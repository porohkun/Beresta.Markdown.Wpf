namespace Beresta.Markdown.Wpf.Kit;

using System.Text.RegularExpressions;
using Extensions;
using Markdig.Extensions.CustomContainers;
using Markdig.Syntax;
using ViewModels;

/// <summary>
/// Контейнеры <c>:::вид Заголовок</c> Docusaurus, VuePress и HackMD: известный вид — плашкой,
/// <c>details</c> и <c>spoiler</c> — сворачиваемым блоком. Заголовок — после имени или в квадратных скобках.
/// </summary>
public sealed partial class CalloutContainers : MdExtension
{
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
