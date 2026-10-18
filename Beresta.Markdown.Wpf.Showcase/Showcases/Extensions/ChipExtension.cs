namespace Beresta.Markdown.Wpf.Showcase.Showcases.Extensions;

using Markdig.Extensions.CustomContainers;
using Markdig.Syntax.Inlines;
using Beresta.Markdown.Wpf.Extensions;
using ViewModels;

/// <summary><c>::текст::</c> — чип: оформленный текст, который остаётся текстом документа.</summary>
public sealed class ChipExtension : MdExtension
{
    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => inline is CustomContainerInline chip
            ? context.ConvertInlines(chip)
                .Select(converted => converted is RunVm run ? run with { StyleName = "Chip", Markup = new("::", "::") } : converted)
                .ToList()
            : null;
}
