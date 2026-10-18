namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>An element of the document model with a position in the source.</summary>
public abstract record NodeVm
{
    /// <summary>Where the element is taken from in the source text.</summary>
    public MdSourceSpan Source { get; init; }

    /// <summary>
    /// A stable identifier: it survives new text while the element has not changed or has changed
    /// in its place. 0 — for an element without its own parse node.
    /// </summary>
    public long NodeId { get; init; }
}