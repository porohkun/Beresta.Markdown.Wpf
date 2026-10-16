namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using Automation;
using ViewModels;

/// <summary>
/// Плашка: цвет, значок и заголовок по виду, вложенные блоки. Вид без своего цвета в теме рисуется
/// цветом и значком заметки, вид без своей подписи — с названием вида в заголовке.
/// </summary>
public class MdCalloutPresenter : Control
{
    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(
            nameof(Kind),
            typeof(string),
            typeof(MdCalloutPresenter),
            new(NoteKind, (d, _) => ((MdCalloutPresenter)d).UpdateKind()));

    /// <summary>Свой заголовок; пусто — <see cref="DefaultTitle"/>.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(IReadOnlyList<InlineVm>),
            typeof(MdCalloutPresenter),
            new(null, (d, e) => d.SetValue(HasTitlePropertyKey, e.NewValue is IReadOnlyList<InlineVm> { Count: > 0 })));

    public static readonly DependencyProperty BlocksProperty =
        DependencyProperty.Register(
            nameof(Blocks),
            typeof(IReadOnlyList<BlockVm>),
            typeof(MdCalloutPresenter));

    public static readonly DependencyProperty IsFoldableProperty =
        DependencyProperty.Register(
            nameof(IsFoldable),
            typeof(bool),
            typeof(MdCalloutPresenter),
            new(false));

    /// <summary>Развёрнута ли сворачиваемая плашка; несворачиваемая развёрнута всегда.</summary>
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(MdCalloutPresenter),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Цвет вида: полоса, значок и заголовок.</summary>
    public static readonly DependencyProperty AccentProperty =
        DependencyProperty.Register(
            nameof(Accent),
            typeof(Brush),
            typeof(MdCalloutPresenter));

    /// <summary>Значок вида.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(Geometry),
            typeof(MdCalloutPresenter));

    /// <summary>Заголовок вида из подписей темы; у вида без своей подписи — название вида.</summary>
    public static readonly DependencyProperty DefaultTitleProperty =
        DependencyProperty.Register(
            nameof(DefaultTitle),
            typeof(string),
            typeof(MdCalloutPresenter));

    private static readonly DependencyPropertyKey HasTitlePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasTitle),
            typeof(bool),
            typeof(MdCalloutPresenter),
            new(false));

    public static readonly DependencyProperty HasTitleProperty = HasTitlePropertyKey.DependencyProperty;

    private const string NoteKind = "note";

    public MdCalloutPresenter()
    {
        UpdateKind();
        Loaded += (_, _) => UpdateKind();
    }

    public string? Kind
    {
        get => (string?)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public IReadOnlyList<InlineVm>? Title
    {
        get => (IReadOnlyList<InlineVm>?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IReadOnlyList<BlockVm>? Blocks
    {
        get => (IReadOnlyList<BlockVm>?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    public bool IsFoldable
    {
        get => (bool)GetValue(IsFoldableProperty);
        set => SetValue(IsFoldableProperty, value);
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public Brush? Accent
    {
        get => (Brush?)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public Geometry? Icon
    {
        get => (Geometry?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? DefaultTitle
    {
        get => (string?)GetValue(DefaultTitleProperty);
        set => SetValue(DefaultTitleProperty, value);
    }

    public bool HasTitle => (bool)GetValue(HasTitleProperty);

    /// <summary>Заголовок текстом: свой или вида.</summary>
    internal string TitleText
        => HasTitle ? string.Concat(Title!.Select(Selection.MdTextMap.TextOf)) : DefaultTitle ?? string.Empty;

    protected override AutomationPeer OnCreateAutomationPeer() => new MdCalloutAutomationPeer(this);

    private static string Capitalize(string kind) => kind.Length == 0 ? kind : char.ToUpperInvariant(kind[0]) + kind[1..];

    /// <summary>Ключи темы вида: <c>Md.Callout.&lt;Вид&gt;.Brush</c>, <c>.Icon</c>, <c>.Title</c>.</summary>
    private void UpdateKind()
    {
        var name = Capitalize((Kind ?? NoteKind).Trim().ToLowerInvariant());
        var look = name.Length > 0 && TryFindResource($"Md.Callout.{name}.Brush") is not null ? name : Capitalize(NoteKind);

        SetResourceReference(AccentProperty, $"Md.Callout.{look}.Brush");
        SetResourceReference(IconProperty, $"Md.Callout.{look}.Icon");
        if (name.Length == 0)
        {
            SetResourceReference(DefaultTitleProperty, $"Md.Callout.{look}.Title");
        }
        else if (TryFindResource($"Md.Callout.{name}.Title") is not null)
        {
            SetResourceReference(DefaultTitleProperty, $"Md.Callout.{name}.Title");
        }
        else
        {
            SetValue(DefaultTitleProperty, name);
        }
    }
}
