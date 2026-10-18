namespace Beresta.Markdown.Wpf.Extensions;

using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Разбор изнутри расширения: вложенная разметка своего узла превращается в модель так же, как
/// остальной документ.
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

    /// <summary>Блоки внутри своего контейнера, с отступами на месте пустых строк.</summary>
    public IReadOnlyList<BlockVm> ConvertChildren(ContainerBlock container)
        => _builder.ConvertBlocks(container, _listDepth);

    /// <summary>Инлайны своего узла.</summary>
    public IReadOnlyList<InlineVm> ConvertInlines(ContainerInline? inlines)
        => _builder.ConvertInlines(inlines);

    /// <summary>Кусок текста с начертанием места узла: жирный, курсив, ссылка вокруг.</summary>
    public RunVm CreateRun(string text)
        => _createRun?.Invoke(text) ?? new RunVm(text);

    /// <summary>Позиция узла в исходнике.</summary>
    public MdSourceSpan GetSpan(MarkdownObject node)
        => _builder.GetSpan(node);

    /// <summary>Текст узла в исходнике как есть.</summary>
    public string GetSource(MarkdownObject node)
        => _builder.GetSource(node);
}