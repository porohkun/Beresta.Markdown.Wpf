namespace MarkdigWpfViewer.Kit;

using System.Text.RegularExpressions;
using Extensions;
using Markdig.Extensions.Alerts;
using Markdig.Syntax;
using ViewModels;

/// <summary>
/// Плашки Obsidian: <c>&gt; [!вид] Заголовок</c>; <c>-</c> или <c>+</c> после вида — сворачиваемая
/// плашка, <c>-</c> — свёрнутая сразу.
/// </summary>
public sealed partial class ObsidianCallouts : MdExtension
{
    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
    {
        switch (block)
        {
            case AlertBlock alert:
                return CalloutKinds.Callout(alert.Kind.ToString(), [], context.ConvertChildren(alert), CalloutKinds.FirstLine(alert, context).Span);

            case QuoteBlock quote
                when quote.FirstOrDefault() is ParagraphBlock paragraph
                     && Marker().Match(context.GetSource(paragraph).Split('\n')[0].TrimEnd('\r')) is { Success: true } marker:
                return FromQuote(quote, marker, context);

            default:
                return null;
        }
    }

    /// <summary>
    /// Плашка из цитаты: первая строка — вид, сворачиваемость и заголовок, остальное — содержимое.
    /// Первый блок не абзац — цитата остаётся цитатой.
    /// </summary>
    private static BlockVm FromQuote(QuoteBlock quote, Match marker, MdConvertContext context)
    {
        var blocks = context.ConvertChildren(quote).ToList();
        if (blocks is not [ParagraphVm first, ..])
        {
            return new QuoteVm(blocks);
        }

        var title = marker.Groups["title"].Success ? marker.Groups["title"].Index : marker.Length;
        var rest = Skip(first.Inlines, title);
        var lineEnd = rest.FindIndex(inline => inline is LineBreakVm);

        var heading = Trim(lineEnd < 0 ? rest : rest[..lineEnd]);
        var body = lineEnd < 0 ? [] : rest[(lineEnd + 1)..];
        if (body.Count > 0)
        {
            blocks[0] = first with { Inlines = body };
        }
        else
        {
            blocks.RemoveAt(0);
        }

        var fold = marker.Groups["fold"].Value;
        return CalloutKinds.Callout(marker.Groups["kind"].Value, heading, blocks, CalloutKinds.FirstLine(quote, context).Span) with
        {
            IsFoldable = fold.Length > 0,
            IsFolded = fold == "-"
        };
    }

    /// <summary>Инлайны без первых <paramref name="count"/> знаков текста.</summary>
    private static List<InlineVm> Skip(IReadOnlyList<InlineVm> inlines, int count)
    {
        var rest = new List<InlineVm>();
        foreach (var inline in inlines)
        {
            if (count > 0 && inline is RunVm run)
            {
                var cut = Math.Min(count, run.Text.Length);
                count -= cut;
                if (cut < run.Text.Length)
                {
                    rest.Add(run with { Text = run.Text[cut..] });
                }

                continue;
            }

            rest.Add(inline);
        }

        return rest;
    }

    /// <summary>Заголовок без пробелов по краям.</summary>
    private static List<InlineVm> Trim(List<InlineVm> title)
    {
        if (title is [RunVm first, ..])
        {
            title[0] = first with { Text = first.Text.TrimStart() };
        }

        if (title is [.., RunVm last])
        {
            title[^1] = last with { Text = last.Text.TrimEnd() };
        }

        return title.Where(inline => inline is not RunVm { Text.Length: 0 }).ToList();
    }

    [GeneratedRegex(@"^\[!(?<kind>[A-Za-z][\w-]*)\](?<fold>[+-]?)(?:[ \t]+(?<title>\S.*))?[ \t]*$")]
    private static partial Regex Marker();
}
