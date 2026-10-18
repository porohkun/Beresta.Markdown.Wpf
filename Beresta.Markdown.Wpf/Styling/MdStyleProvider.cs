namespace Beresta.Markdown.Wpf.Styling;

using System.Windows;
using Abstractions;

/// <summary>
/// Templates of built-in blocks. A template not set is taken from <see cref="BasedOn"/>: in XAML
/// one template is replaced, the rest stay standard.
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

    /// <summary>The provider that templates not set are taken from.</summary>
    public IMdStyleProvider? BasedOn { get; set; }

    /// <inheritdoc/>
    public DataTemplate? ParagraphTemplate
    {
        get => _paragraph ?? BasedOn?.ParagraphTemplate;
        set => _paragraph = value;
    }

    /// <inheritdoc/>
    public DataTemplate? HeadingTemplate
    {
        get => _heading ?? BasedOn?.HeadingTemplate;
        set => _heading = value;
    }

    /// <inheritdoc/>
    public DataTemplate? CodeBlockTemplate
    {
        get => _codeBlock ?? BasedOn?.CodeBlockTemplate;
        set => _codeBlock = value;
    }

    /// <inheritdoc/>
    public DataTemplate? QuoteTemplate
    {
        get => _quote ?? BasedOn?.QuoteTemplate;
        set => _quote = value;
    }

    /// <inheritdoc/>
    public DataTemplate? ListTemplate
    {
        get => _list ?? BasedOn?.ListTemplate;
        set => _list = value;
    }

    /// <inheritdoc/>
    public DataTemplate? TableTemplate
    {
        get => _table ?? BasedOn?.TableTemplate;
        set => _table = value;
    }

    /// <inheritdoc/>
    public DataTemplate? HorizontalRuleTemplate
    {
        get => _horizontalRule ?? BasedOn?.HorizontalRuleTemplate;
        set => _horizontalRule = value;
    }

    /// <inheritdoc/>
    public DataTemplate? BlankLineTemplate
    {
        get => _blankLine ?? BasedOn?.BlankLineTemplate;
        set => _blankLine = value;
    }

    /// <inheritdoc/>
    public DataTemplate? FootnoteTemplate
    {
        get => _footnote ?? BasedOn?.FootnoteTemplate;
        set => _footnote = value;
    }

    /// <inheritdoc/>
    public DataTemplate? ImageTemplate
    {
        get => _image ?? BasedOn?.ImageTemplate;
        set => _image = value;
    }

    /// <inheritdoc/>
    public DataTemplate? TaskTemplate
    {
        get => _task ?? BasedOn?.TaskTemplate;
        set => _task = value;
    }

    /// <inheritdoc/>
    public DataTemplate? FootnoteLinkTemplate
    {
        get => _footnoteLink ?? BasedOn?.FootnoteLinkTemplate;
        set => _footnoteLink = value;
    }

    /// <inheritdoc/>
    public DataTemplate? FigureTemplate
    {
        get => _figure ?? BasedOn?.FigureTemplate;
        set => _figure = value;
    }

    /// <inheritdoc/>
    public DataTemplate? FigureCaptionTemplate
    {
        get => _figureCaption ?? BasedOn?.FigureCaptionTemplate;
        set => _figureCaption = value;
    }

    /// <inheritdoc/>
    public DataTemplate? FooterTemplate
    {
        get => _footer ?? BasedOn?.FooterTemplate;
        set => _footer = value;
    }

    /// <inheritdoc/>
    public DataTemplate? CalloutTemplate
    {
        get => _callout ?? BasedOn?.CalloutTemplate;
        set => _callout = value;
    }

    /// <inheritdoc/>
    public DataTemplate? DetailsTemplate
    {
        get => _details ?? BasedOn?.DetailsTemplate;
        set => _details = value;
    }

    /// <inheritdoc/>
    public DataTemplate? DefinitionListTemplate
    {
        get => _definitionList ?? BasedOn?.DefinitionListTemplate;
        set => _definitionList = value;
    }

    /// <inheritdoc/>
    public DataTemplate? AlignedTemplate
    {
        get => _aligned ?? BasedOn?.AlignedTemplate;
        set => _aligned = value;
    }
}