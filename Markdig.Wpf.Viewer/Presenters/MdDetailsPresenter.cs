namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Automation;
using ViewModels;

/// <summary>Сворачиваемый блок: заголовок-переключатель и вложенные блоки.</summary>
public class MdDetailsPresenter : Control
{
    /// <summary>Заголовок; пусто — <see cref="DefaultTitle"/>.</summary>
    public static readonly DependencyProperty SummaryProperty =
        DependencyProperty.Register(
            nameof(Summary),
            typeof(IReadOnlyList<InlineVm>),
            typeof(MdDetailsPresenter),
            new(null, (d, e) => d.SetValue(HasSummaryPropertyKey, e.NewValue is IReadOnlyList<InlineVm> { Count: > 0 })));

    public static readonly DependencyProperty BlocksProperty =
        DependencyProperty.Register(
            nameof(Blocks),
            typeof(IReadOnlyList<BlockVm>),
            typeof(MdDetailsPresenter));

    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(MdDetailsPresenter),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Заголовок блока без своего заголовка — из подписей темы.</summary>
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

    public static readonly DependencyProperty HasSummaryProperty = HasSummaryPropertyKey.DependencyProperty;

    public IReadOnlyList<InlineVm>? Summary
    {
        get => (IReadOnlyList<InlineVm>?)GetValue(SummaryProperty);
        set => SetValue(SummaryProperty, value);
    }

    public IReadOnlyList<BlockVm>? Blocks
    {
        get => (IReadOnlyList<BlockVm>?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public string? DefaultTitle
    {
        get => (string?)GetValue(DefaultTitleProperty);
        set => SetValue(DefaultTitleProperty, value);
    }

    public bool HasSummary => (bool)GetValue(HasSummaryProperty);

    /// <summary>Заголовок текстом: свой или из подписей темы.</summary>
    internal string TitleText
        => HasSummary ? string.Concat(Summary!.Select(Selection.MdTextMap.TextOf)) : DefaultTitle ?? string.Empty;

    protected override AutomationPeer OnCreateAutomationPeer() => new MdDetailsAutomationPeer(this);
}
