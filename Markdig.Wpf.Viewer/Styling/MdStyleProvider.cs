namespace MarkdigWpfViewer.Styling;

using System.Windows;
using Abstractions;

/// <summary>
/// Шаблоны встроенных блоков. Незаданный шаблон берётся у <see cref="BasedOn"/>: в XAML
/// заменяется один шаблон, остальные остаются стандартными.
/// </summary>
public sealed class MdStyleProvider : IMdStyleProvider
{
    private DataTemplate? _paragraph;
    private DataTemplate? _heading;
    private DataTemplate? _codeBlock;
    private DataTemplate? _quote;
    private DataTemplate? _list;
    private DataTemplate? _table;
    private DataTemplate? _horizontalRule;
    private DataTemplate? _blankLine;

    /// <summary>Провайдер, у которого берутся незаданные шаблоны.</summary>
    public IMdStyleProvider? BasedOn { get; set; }

    public DataTemplate? ParagraphTemplate
    {
        get => _paragraph ?? BasedOn?.ParagraphTemplate;
        set => _paragraph = value;
    }

    public DataTemplate? HeadingTemplate
    {
        get => _heading ?? BasedOn?.HeadingTemplate;
        set => _heading = value;
    }

    public DataTemplate? CodeBlockTemplate
    {
        get => _codeBlock ?? BasedOn?.CodeBlockTemplate;
        set => _codeBlock = value;
    }

    public DataTemplate? QuoteTemplate
    {
        get => _quote ?? BasedOn?.QuoteTemplate;
        set => _quote = value;
    }

    public DataTemplate? ListTemplate
    {
        get => _list ?? BasedOn?.ListTemplate;
        set => _list = value;
    }

    public DataTemplate? TableTemplate
    {
        get => _table ?? BasedOn?.TableTemplate;
        set => _table = value;
    }

    public DataTemplate? HorizontalRuleTemplate
    {
        get => _horizontalRule ?? BasedOn?.HorizontalRuleTemplate;
        set => _horizontalRule = value;
    }

    public DataTemplate? BlankLineTemplate
    {
        get => _blankLine ?? BasedOn?.BlankLineTemplate;
        set => _blankLine = value;
    }
}
