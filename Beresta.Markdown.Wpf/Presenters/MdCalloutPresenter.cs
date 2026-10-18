namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using Automation;
using ViewModels;

/// <summary>
/// A callout: color, icon and title by kind, nested blocks. A kind without its own color in the
/// theme is drawn with the color and icon of a note, a kind without its own caption — with the name
/// of the kind in the title.
/// </summary>
public class MdCalloutPresenter : Control
{
    /// <summary>The kind of the callout: <c>note</c>, <c>tip</c>, <c>warning</c> and others.</summary>
    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(
            nameof(Kind),
            typeof(string),
            typeof(MdCalloutPresenter),
            new(NoteKind, (d, _) => ((MdCalloutPresenter)d).UpdateKind()));

    /// <summary>Its own title; empty — <see cref="DefaultTitle"/>.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(IReadOnlyList<InlineVm>),
            typeof(MdCalloutPresenter),
            new(null, (d, e) => d.SetValue(HasTitlePropertyKey, e.NewValue is IReadOnlyList<InlineVm> { Count: > 0 })));

    /// <summary>The blocks inside the callout.</summary>
    public static readonly DependencyProperty BlocksProperty =
        DependencyProperty.Register(
            nameof(Blocks),
            typeof(IReadOnlyList<BlockVm>),
            typeof(MdCalloutPresenter));

    /// <summary>The callout can be collapsed.</summary>
    public static readonly DependencyProperty IsFoldableProperty =
        DependencyProperty.Register(
            nameof(IsFoldable),
            typeof(bool),
            typeof(MdCalloutPresenter),
            new(false));

    /// <summary>Whether a collapsible callout is expanded; a non-collapsible one is always expanded.</summary>
    public static readonly DependencyProperty IsExpandedProperty =
        DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(MdCalloutPresenter),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>The color of the kind: the bar, the icon and the title.</summary>
    public static readonly DependencyProperty AccentProperty =
        DependencyProperty.Register(
            nameof(Accent),
            typeof(Brush),
            typeof(MdCalloutPresenter));

    /// <summary>The icon of the kind.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(Geometry),
            typeof(MdCalloutPresenter));

    /// <summary>
    /// The title of the kind from the captions of the theme; a kind without its own caption gets
    /// its name.
    /// </summary>
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

    /// <summary>The callout has its own title.</summary>
    public static readonly DependencyProperty HasTitleProperty = HasTitlePropertyKey.DependencyProperty;

    private const string NoteKind = "note";

    /// <summary>Creates an empty callout.</summary>
    public MdCalloutPresenter()
    {
        UpdateKind();
        Loaded += (_, _) => UpdateKind();
    }

    /// <inheritdoc cref="KindProperty"/>
    public string? Kind
    {
        get => (string?)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <inheritdoc cref="TitleProperty"/>
    public IReadOnlyList<InlineVm>? Title
    {
        get => (IReadOnlyList<InlineVm>?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <inheritdoc cref="BlocksProperty"/>
    public IReadOnlyList<BlockVm>? Blocks
    {
        get => (IReadOnlyList<BlockVm>?)GetValue(BlocksProperty);
        set => SetValue(BlocksProperty, value);
    }

    /// <inheritdoc cref="IsFoldableProperty"/>
    public bool IsFoldable
    {
        get => (bool)GetValue(IsFoldableProperty);
        set => SetValue(IsFoldableProperty, value);
    }

    /// <inheritdoc cref="IsExpandedProperty"/>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <inheritdoc cref="AccentProperty"/>
    public Brush? Accent
    {
        get => (Brush?)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <inheritdoc cref="IconProperty"/>
    public Geometry? Icon
    {
        get => (Geometry?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <inheritdoc cref="DefaultTitleProperty"/>
    public string? DefaultTitle
    {
        get => (string?)GetValue(DefaultTitleProperty);
        set => SetValue(DefaultTitleProperty, value);
    }

    /// <inheritdoc cref="HasTitleProperty"/>
    public bool HasTitle => (bool)GetValue(HasTitleProperty);

    /// <summary>Заголовок текстом: свой или вида.</summary>
    internal string TitleText
        => HasTitle ? string.Concat(Title!.Select(Selection.MdTextMap.TextOf)) : DefaultTitle ?? string.Empty;

    /// <inheritdoc/>
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
