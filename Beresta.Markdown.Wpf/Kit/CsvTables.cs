namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig.Syntax;
using ViewModels;

/// <summary>
/// Tables from CSV: a code block with the language <c>csv</c> or <c>tsv</c> is drawn as a table of
/// the core, the first record is the header. Quoted values follow the rules of CSV.
/// </summary>
public sealed class CsvTables : MdExtension
{
    /// <inheritdoc/>
    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
    {
        if (block is not FencedCodeBlock code || DelimiterOf(code.Info) is not { } delimiter)
        {
            return null;
        }

        var lines = KitLines.Of(code.Lines);
        var records = CsvReader.Read(lines.Select(line => line.ToString()).ToList(), delimiter);
        if (records.Count == 0)
        {
            return null;
        }

        var columns = records.Max(record => record.Fields.Count);
        var rows = records
            .Select((record, index) => new TableRowVm(index == 0, Cells(record.Fields, columns)) { Source = KitLines.SpanOf(lines[record.First], lines[record.Last]) })
            .ToList();
        return new TableVm(columns, rows);
    }

    /// <inheritdoc/>
    public override void DescribeSource(NodeVm node, MdSourceLook look)
    {
        if (node is not TableVm { Source: { IsEmpty: false } span } table || span.End > look.Text.Length)
        {
            return;
        }

        var fence = look.Text.AsSpan(span.Start, span.Length);
        var info = fence[..Math.Max(0, fence.IndexOfAny('\r', '\n'))].TrimStart().TrimStart("`~").Trim();
        if (DelimiterOf(info.ToString()) is not { } delimiter)
        {
            return;
        }

        var rows = new List<(int Line, IReadOnlyList<int> Separators)>();
        foreach (var row in table.Rows)
        {
            if (row.Source is not { IsEmpty: false } record || record.StartLine != record.EndLine || record.End > look.Text.Length)
            {
                continue;
            }

            var separators = new List<int>();
            var quoted = false;
            for (var at = record.Start; at < record.End; at++)
            {
                if (look.Text[at] == '"')
                {
                    quoted = !quoted;
                }
                else if (look.Text[at] == delimiter && !quoted)
                {
                    separators.Add(at);
                }
            }

            rows.Add((record.StartLine, separators));
        }

        look.MarkColumns(rows, []);
    }

    private static char? DelimiterOf(string? language)
        => language?.ToLowerInvariant() switch
        {
            "csv" => ',',
            "tsv" => '\t',
            _ => null
        };

    private static List<TableCellVm> Cells(List<string> fields, int columns)
        => fields
            .Select(field => new TableCellVm(KitLines.Runs(field)))
            .Concat(Enumerable.Range(0, columns - fields.Count).Select(_ => new TableCellVm([])))
            .ToList();
}
