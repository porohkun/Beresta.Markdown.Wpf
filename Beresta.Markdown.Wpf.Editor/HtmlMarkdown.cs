namespace Beresta.Markdown.Wpf.Editor;

using System.Text;
using System.Text.RegularExpressions;
using ReverseMarkdown;

/// <summary>
/// HTML из буфера обмена — в Markdown: фрагмент формата <c>HTML Format</c> переводит ReverseMarkdown —
/// таблицы видом GitHub, списки, код, ссылки, картинки, заголовки; незнакомые теги — их текстом.
/// </summary>
internal static partial class HtmlMarkdown
{
    /// <summary>Markdown из данных формата <c>HTML Format</c>.</summary>
    public static string Convert(string data)
    {
        var converter = new Converter(new Config
        {
            Tags = { Unknown = Config.UnknownTagsOption.Bypass },
            Formatting = { RemoveComments = true, ListBulletChar = '-' },
            Links = { SmartHref = true }
        });
        return converter.Convert(Fragment(data)).Trim();
    }

    /// <summary>
    /// Фрагмент данных <c>HTML Format</c>: между метками <c>StartFragment</c> и <c>EndFragment</c>, а без
    /// них — по смещениям в байтах из заголовка; без заголовка — весь текст.
    /// </summary>
    private static string Fragment(string data)
    {
        const string start = "<!--StartFragment-->";
        const string end = "<!--EndFragment-->";
        var from = data.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        var to = data.IndexOf(end, StringComparison.OrdinalIgnoreCase);
        if (from >= 0 && to > from)
        {
            return data[(from + start.Length)..to];
        }

        if (Offset().Match(data) is { Success: true } header)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var first = int.Parse(header.Groups["start"].Value);
            var last = int.Parse(header.Groups["end"].Value);
            if (first >= 0 && last > first && last <= bytes.Length)
            {
                return Encoding.UTF8.GetString(bytes, first, last - first);
            }
        }

        return data;
    }

    [GeneratedRegex(@"StartFragment:(?<start>\d+)\s+EndFragment:(?<end>\d+)")]
    private static partial Regex Offset();
}
