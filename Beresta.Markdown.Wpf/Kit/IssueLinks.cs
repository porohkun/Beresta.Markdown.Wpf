namespace Beresta.Markdown.Wpf.Kit;

using System.Text.RegularExpressions;
using Extensions;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Issue links by templates of the application: <c>#123</c>, <c>org/repo#45</c>, <c>PROJ-45</c>. A
/// template is a prefix as written before the number and an address with <c>{0}</c> in place of the
/// number. There are no templates by default.
/// </summary>
public sealed partial class IssueLinks : MdExtension
{
    /// <summary>
    /// Templates: the prefix of an issue — <c>#</c>, <c>org/repo#</c>, <c>PROJ-</c> — and an
    /// address with <c>{0}</c> in place of the number.
    /// </summary>
    public Dictionary<string, string> Templates { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
    {
        if (Templates.Count == 0 || inline is not LiteralInline literal)
        {
            return null;
        }

        var text = literal.Content.ToString();
        var references = Reference()
            .Matches(text)
            .Where(match => Templates.ContainsKey(match.Groups["prefix"].Value))
            .ToList();
        if (references.Count == 0 || context.CreateRun(string.Empty).Href is not null)
        {
            return null;
        }

        var result = new List<InlineVm>();
        var position = 0;
        foreach (var reference in references)
        {
            if (reference.Index > position)
            {
                result.Add(context.CreateRun(text[position..reference.Index]));
            }

            var href = Templates[reference.Groups["prefix"].Value].Replace("{0}", reference.Groups["number"].Value);
            result.Add(context.CreateRun(reference.Value) with
            {
                Href = href,
                Hyperlink = KitLinks.UriOf(href),
                Markup = new(string.Empty, string.Empty) { ReplacesLink = true }
            });
            position = reference.Index + reference.Length;
        }

        if (position < text.Length)
        {
            result.Add(context.CreateRun(text[position..]));
        }

        return result;
    }

    [GeneratedRegex(@"(?<![\w/#.-])(?<prefix>(?:[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+)?#|[A-Z][A-Z0-9]+-)(?<number>\d+)(?![\w-])")]
    private static partial Regex Reference();
}
