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
    private DataTemplate? _footnote;
    private DataTemplate? _image;
    private DataTemplate? _task;
    private DataTemplate? _footnoteLink;
    private DataTemplate? _figure;
    private DataTemplate? _figureCaption;
    private DataTemplate? _footer;
    private DataTemplate? _callout;
    private DataTemplate? _details;
    private DataTemplate? _definitionList;
    private DataTemplate? _aligned;

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

    public DataTemplate? FootnoteTemplate
    {
        get => _footnote ?? BasedOn?.FootnoteTemplate;
        set => _footnote = value;
    }

    public DataTemplate? ImageTemplate
    {
        get => _image ?? BasedOn?.ImageTemplate;
        set => _image = value;
    }

    public DataTemplate? TaskTemplate
    {
        get => _task ?? BasedOn?.TaskTemplate;
        set => _task = value;
    }

    public DataTemplate? FootnoteLinkTemplate
    {
        get => _footnoteLink ?? BasedOn?.FootnoteLinkTemplate;
        set => _footnoteLink = value;
    }

    public DataTemplate? FigureTemplate
    {
        get => _figure ?? BasedOn?.FigureTemplate;
        set => _figure = value;
    }

    public DataTemplate? FigureCaptionTemplate
    {
        get => _figureCaption ?? BasedOn?.FigureCaptionTemplate;
        set => _figureCaption = value;
    }

    public DataTemplate? FooterTemplate
    {
        get => _footer ?? BasedOn?.FooterTemplate;
        set => _footer = value;
    }

    public DataTemplate? CalloutTemplate
    {
        get => _callout ?? BasedOn?.CalloutTemplate;
        set => _callout = value;
    }

    public DataTemplate? DetailsTemplate
    {
        get => _details ?? BasedOn?.DetailsTemplate;
        set => _details = value;
    }

    public DataTemplate? DefinitionListTemplate
    {
        get => _definitionList ?? BasedOn?.DefinitionListTemplate;
        set => _definitionList = value;
    }

    public DataTemplate? AlignedTemplate
    {
        get => _aligned ?? BasedOn?.AlignedTemplate;
        set => _aligned = value;
    }
}