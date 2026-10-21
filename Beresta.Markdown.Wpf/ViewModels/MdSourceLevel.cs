namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>
/// Editable units of one container around a source line: <paramref name="Container"/> — the place of
/// the container, <paramref name="Units"/> — the places of its units in order, <paramref name="Indent"/>
/// — how many characters at the start of their lines the container draws its own way.
/// </summary>
public sealed record MdSourceLevel(MdSourceSpan Container, IReadOnlyList<MdSourceSpan> Units, int Indent);
