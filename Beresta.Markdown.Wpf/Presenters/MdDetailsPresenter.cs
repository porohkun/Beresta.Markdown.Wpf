namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Automation;
using ViewModels;

/// <summary>A collapsible block: a toggle title and nested blocks.</summary>
public class MdDetailsPresenter : Control
{
    /// <summary>The title; empty — <see cref="DefaultTitle"/>.</summary>
    public static readonly DependencyProperty SummaryProperty =
        DependencyProperty.Register(
            nameof(Summary),
            typeof(IReadOnlyList<InlineVm>),
            typeof(MdDetailsPresenter),
            new(null, (d, e) => d.SetValue(HasSummaryPropertyKey, e.NewValue is IReadOnlyList<InlineVm> { Count: > 0 })));

    /// <summary>The blocks inside the collapsible block.</summary>
    public static readonly DependencyProperty BlocksProperty =
        DependencyProperty.Register(
            nameof(Blocks),
            typeof(IReadOnlyList<BlockVm>),
            typeof(MdDetailsPresenter));

    /// <summary>The block is expanded.</summary>
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(MdDetailsPresenter),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>The title of a block without its own title, from the captions of the theme.</summary>
    public static readonly DependencyProperty DefaultTitleProperty =
        DependencyProperty.Register(
            nameof(DefaultTitle),
            typeof(string),
            typeof(MdDetailsPresenter));

    private static readonly DependencyPropertyKey HasSummaryPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasSummary),
            typeof(bool),
            typeof(MdDetailsPresenter),
            new(false));

    /// <summary>The block has its own title.</summary>
    public static readonly DependencyProperty HasSummaryProperty = HasSummaryPropertyKey.DependencyProperty;

    /// <inheritdoc cref="SummaryProperty"/>
    public IReadOnlyList<InlineVm>? Summary
    {
        get => (IReadOnlyList<InlineVm>?)GetValue(SummaryProperty);
        set => SetValue(SummaryProperty, value);
    }

    /// <inheritdoc cref="BlocksProperty"/>
    public IReadOnlyList<BlockVm>? Blocks
    {
        get => (IReadOnlyList<BlockVm>?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    /// <inheritdoc cref="IsExpandedProperty"/>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <inheritdoc cref="DefaultTitleProperty"/>
    public string? DefaultTitle
    {
        get => (string?)GetValue(DefaultTitleProperty);
        set => SetValue(DefaultTitleProperty, value);
    }

    /// <inheritdoc cref="HasSummaryProperty"/>
    public bool HasSummary => (bool)GetValue(HasSummaryProperty);

    /// <summary>Заголовок текстом: свой или из подписей темы.</summary>
    internal string TitleText
        => HasSummary ? string.Concat(Summary!.Select(Selection.MdTextMap.TextOf)) : DefaultTitle ?? string.Empty;

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new MdDetailsAutomationPeer(this);
}
