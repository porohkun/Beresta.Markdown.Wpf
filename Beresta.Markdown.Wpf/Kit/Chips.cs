namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Chips <c>::text::</c> and <c>::text::{.kind}</c> as styled text <c>Chip</c> or <c>Chip.Kind</c>
/// for the kinds <c>info</c>, <c>success</c>, <c>warning</c>, <c>danger</c>.
/// </summary>
public sealed class Chips : MdExtension
{
    private static readonly HashSet<string> Kinds = new(StringComparer.OrdinalIgnoreCase) { "info", "success", "warning", "danger" };

    /// <inheritdoc/>
    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
    {
        if (inline is not CustomContainerInline chip)
        {
            return null;
        }

        var kind = chip.TryGetAttributes()?.Classes?.FirstOrDefault();
        var style = kind is not null && Kinds.Contains(kind) ? "Chip." + char.ToUpperInvariant(kind[0]) + kind[1..].ToLowerInvariant() : "Chip";
        var frame = new MarkupFrame("::", kind is null ? "::" : "::{." + kind + "}");

        return context.ConvertInlines(chip)
            .Select(converted => converted is RunVm run ? run with { StyleName = style, Markup = frame } : converted)
            .ToList();
    }
}
