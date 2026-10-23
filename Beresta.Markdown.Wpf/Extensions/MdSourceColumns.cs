namespace Beresta.Markdown.Wpf.Extensions;

using ViewModels;

/// <summary>
/// Columns of a table in the source: <paramref name="Rows"/> — source lines, counting from zero, each with
/// the places of the separators that end its cells; <paramref name="Alignments"/> — the alignment of the
/// columns.
/// </summary>
public sealed record MdSourceColumns(IReadOnlyList<(int Line, IReadOnlyList<int> Separators)> Rows, IReadOnlyList<TableAlignment> Alignments);
