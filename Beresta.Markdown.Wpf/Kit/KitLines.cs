namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Helpers;
using ViewModels;

/// <summary>Строки блока для готовых расширений: текст, позиция в исходнике, текст инлайнами.</summary>
internal static class KitLines
{
    /// <summary>Строки группы по порядку.</summary>
    public static List<StringLine> Of(StringLineGroup lines)
        => Enumerable.Range(0, lines.Count)
            .Select(index => lines.Lines[index])
            .ToList();

    /// <summary>Кусок исходника от начала строки <paramref name="first"/> до конца строки <paramref name="last"/>.</summary>
    public static MdSourceSpan SpanOf(StringLine first, StringLine last)
        => new(first.Slice.Start, Math.Max(last.Slice.End - first.Slice.Start + 1, 1), first.Line, last.Line);

    /// <summary>Текст инлайнами: строки — кусками текста, между ними — переносы.</summary>
    public static IReadOnlyList<InlineVm> Runs(string text)
        => text.Length == 0
            ? []
            : text.Split('\n')
                .SelectMany((line, index) => index == 0 ? new InlineVm[] { new RunVm(line) } : [new LineBreakVm(), new RunVm(line)])
                .Where(inline => inline is not RunVm { Text.Length: 0 })
                .ToList();
}
