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
