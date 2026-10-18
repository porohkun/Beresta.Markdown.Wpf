namespace Beresta.Markdown.Wpf.Extensions;

using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Parsing from inside an extension: the nested markup of its node becomes model the same way as
/// the rest of the document.
/// </summary>
public sealed class MdConvertContext
{
    private readonly ModelBuilder _builder;
    private readonly int _listDepth;
    private readonly Func<string, RunVm>? _createRun;

    internal MdConvertContext(ModelBuilder builder, int listDepth, Func<string, RunVm>? createRun = null)
    {
        _builder = builder;
        _listDepth = listDepth;
        _createRun = createRun;
    }

    /// <summary>The blocks inside its container, with spaces in place of blank lines.</summary>
    public IReadOnlyList<BlockVm> ConvertChildren(ContainerBlock container)
        => _builder.ConvertBlocks(container, _listDepth);

    /// <summary>The inlines of its node.</summary>
    public IReadOnlyList<InlineVm> ConvertInlines(ContainerInline? inlines)
        => _builder.ConvertInlines(inlines);

    /// <summary>A piece of text with the formatting of the place of the node: bold, italic, a link around.</summary>
    public RunVm CreateRun(string text)
        => _createRun?.Invoke(text) ?? new RunVm(text);

    /// <summary>The position of a node in the source.</summary>
    public MdSourceSpan GetSpan(MarkdownObject node)
        => _builder.GetSpan(node);

    /// <summary>The text of a node in the source as is.</summary>
    public string GetSource(MarkdownObject node)
        => _builder.GetSource(node);
}