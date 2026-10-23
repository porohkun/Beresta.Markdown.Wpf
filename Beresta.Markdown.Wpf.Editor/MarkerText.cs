namespace Beresta.Markdown.Wpf.Editor;

using ICSharpCode.AvalonEdit.Rendering;

/// <summary>Маркер строки — пункта, цитаты: AvalonEdit считает его пробелом, и перенесённые строки начинаются после него.</summary>
internal sealed class MarkerText(VisualLine line, int length) : VisualLineText(line, length)
{
    /// <inheritdoc/>
    public override bool IsWhitespace(int visualColumn) => true;

    /// <inheritdoc/>
    protected override VisualLineText CreateInstance(int length) => new MarkerText(ParentVisualLine, length);
}
