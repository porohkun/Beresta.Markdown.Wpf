namespace Beresta.Markdown.Wpf.Extensions;

using System.Windows;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// An extension of the viewer for your own syntax: parser setup, turning your Markdig nodes into
/// widgets and templates for them. Connected through <see cref="MarkdownViewer.Extensions"/> or
/// <see cref="MarkdownViewer.SharedExtensions"/>.
/// </summary>
public abstract class MdExtension
{
    /// <summary>
    /// The extension may be called from a background thread when a long text is parsed there;
    /// <c>false</c> — while it is connected, the viewer parses every text on the interface thread.
    /// </summary>
    public virtual bool AllowsBackgroundParse => true;

    /// <summary>Adds your Markdig extensions to parsing.</summary>
    public virtual void SetupPipeline(MarkdownPipelineBuilder pipeline)
    {
    }

    /// <summary>Turns a node into a block widget; <c>null</c> — the node is not yours.</summary>
    public virtual BlockVm? ConvertBlock(Block block, MdConvertContext context) => null;

    /// <summary>
    /// Turns a node into several blocks of the model; an empty list — the node is not drawn,
    /// <c>null</c> — the node is not yours. By default <see cref="ConvertBlock"/>.
    /// </summary>
    public virtual IReadOnlyList<BlockVm>? ConvertBlocks(Block block, MdConvertContext context)
        => ConvertBlock(block, context) is { } single ? [single] : null;

    /// <summary>Turns an inline into an inline widget; <c>null</c> — the inline is not yours.</summary>
    public virtual InlineVm? ConvertInline(Inline inline, MdConvertContext context) => null;

    /// <summary>
    /// Turns an inline into several inlines of the model: text, styled pieces, widgets; an empty
    /// list — the inline is not drawn, <c>null</c> — the inline is not yours. By default
    /// <see cref="ConvertInline"/>.
    /// </summary>
    public virtual IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => ConvertInline(inline, context) is { } single ? [single] : null;

    /// <summary>The template of your widget; <c>null</c> — the item is not yours.</summary>
    public virtual DataTemplate? SelectTemplate(object item, DependencyObject container) => null;

    /// <summary>
    /// The state of your widget that must outlive its control: collapsed or not, what is selected.
    /// The template gets it through <see cref="MdWidget.StateProperty"/>; <c>null</c> — no state.
    /// </summary>
    public virtual object? CreateState(NodeVm node) => null;

    /// <summary>
    /// What to write into the source when your widget is pressed (<paramref name="state"/> is
    /// <c>null</c>) or its state changed (the new state); <c>null</c> — nothing.
    /// </summary>
    public virtual MdSourceEdit? EditSource(NodeVm node, object? state, MdSourceContext context) => null;

    /// <summary>
    /// How your widget looks in the source of an editor: marks its pieces and lines with looks. By
    /// default — nothing: the source of the widget is plain text.
    /// </summary>
    public virtual void DescribeSource(NodeVm node, MdSourceLook look)
    {
    }
}