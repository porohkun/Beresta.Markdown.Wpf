namespace MarkdigWpfViewer.Showcase.Showcases.Sets;

using MarkdigWpfViewer.Extensions;

/// <summary>Диалект: название, документ в его синтаксисе и набор, который этот синтаксис рисует.</summary>
public sealed record Dialect(string Title, string Text, MdExtension Set);
