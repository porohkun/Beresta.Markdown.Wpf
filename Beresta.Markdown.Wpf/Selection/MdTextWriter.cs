namespace Beresta.Markdown.Wpf.Selection;

using ViewModels;

/// <summary>Writes the text of a widget to the text map, in reading order.</summary>
public sealed class MdTextWriter
{
    private readonly Action<string> _text;
    private readonly Action<IReadOnlyList<InlineVm>> _inlines;
    private readonly Action<IReadOnlyList<BlockVm>> _blocks;

    internal MdTextWriter(MdTextMap.Builder builder)
        : this(builder.Append, builder.AppendInlines, builder.AppendBlocks)
    {
    }

    internal MdTextWriter(Action<string> text, Action<IReadOnlyList<InlineVm>> inlines, Action<IReadOnlyList<BlockVm>> blocks)
    {
        _text = text;
        _inlines = inlines;
        _blocks = blocks;
    }

    /// <summary>Text that is not in inlines: it is copied, but no presenter highlights it.</summary>
    public void Text(string text) => _text(text);

    /// <summary>Inlines the widget draws through <c>MdTextBlock</c> with this same list.</summary>
    public void Inlines(IReadOnlyList<InlineVm> inlines) => _inlines(inlines);

    /// <summary>Nested blocks — the same way as blocks of the document.</summary>
    public void Blocks(IReadOnlyList<BlockVm> blocks) => _blocks(blocks);
}