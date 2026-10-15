namespace MarkdigWpfViewer;

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Abstractions;
using Extensions;
using Markdig;
using ViewModels;

/// <summary>Контрол, который показывает markdown-текст блоками по шаблонам темы.</summary>
public sealed class MarkdownViewer : ItemsControl
{
    /// <summary>Текст документа.</summary>
    public static readonly DependencyProperty MarkdownProperty =
        DependencyProperty.Register(
            nameof(Markdown),
            typeof(string),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.None, OnMarkdownChanged));

    /// <summary>Базовая настройка разбора Markdig; не задана — расширенная по умолчанию.</summary>
    public static readonly DependencyProperty MarkdownPipelineProperty =
        DependencyProperty.Register(
            nameof(MarkdownPipeline),
            typeof(MarkdownPipeline),
            typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(null, OnPipelineChanged));

    /// <summary>Шаблоны встроенных блоков.</summary>
    public static readonly DependencyProperty StyleProviderProperty =
        DependencyProperty.Register(
            nameof(StyleProvider),
            typeof(IMdStyleProvider),
            typeof(MarkdownViewer),
            new(null, OnStyleProviderChanged));

    /// <summary>Расширения под свой синтаксис.</summary>
    public static readonly DependencyProperty ExtensionsProperty =
        DependencyProperty.Register(
            nameof(Extensions),
            typeof(MdExtensionCollection),
            typeof(MarkdownViewer),
            new(null, OnExtensionsChanged));

    private MarkdownPipeline? _pipeline;

    static MarkdownViewer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MarkdownViewer), new FrameworkPropertyMetadata(typeof(MarkdownViewer)));
    }

    public MarkdownViewer()
    {
        // BYDESIGN: своя коллекция через SetCurrentValue, а не локальным значением — так её
        // перекрывает общая из стиля, а XAML, который читает свойство мимо геттера, видит не null.
        SetCurrentValue(ExtensionsProperty, new MdExtensionCollection());
    }

    public string? Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public MarkdownPipeline? MarkdownPipeline
    {
        get => (MarkdownPipeline?)GetValue(MarkdownPipelineProperty);
        set => SetValue(MarkdownPipelineProperty, value);
    }

    public IMdStyleProvider? StyleProvider
    {
        get => (IMdStyleProvider?)GetValue(StyleProviderProperty);
        set => SetValue(StyleProviderProperty, value);
    }

    /// <summary>Расширения вьюера; общую коллекцию на несколько вьюеров можно задать стилем.</summary>
    public MdExtensionCollection Extensions
    {
        get => (MdExtensionCollection)GetValue(ExtensionsProperty);
        set => SetValue(ExtensionsProperty, value);
    }

    internal IReadOnlyList<MdExtension> ActiveExtensions
        => GetValue(ExtensionsProperty) as MdExtensionCollection ?? (IReadOnlyList<MdExtension>)[];

    private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).Rebuild();

    private static void OnPipelineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).ResetPipeline();

    private static void OnStyleProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarkdownViewer)d).Rebuild();

    private static void OnExtensionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (MarkdownViewer)d;

        if (e.OldValue is MdExtensionCollection oldExtensions)
        {
            oldExtensions.CollectionChanged -= viewer.OnExtensionsCollectionChanged;
        }

        if (e.NewValue is MdExtensionCollection newExtensions)
        {
            newExtensions.CollectionChanged += viewer.OnExtensionsCollectionChanged;
        }

        viewer.ResetPipeline();
    }

    private void OnExtensionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ResetPipeline();

    private void ResetPipeline()
    {
        _pipeline = null;
        Rebuild();
    }

    private MarkdownPipeline GetPipeline()
    {
        if (_pipeline is not null)
        {
            return _pipeline;
        }

        var extensions = ActiveExtensions;
        if (extensions.Count == 0)
        {
            return _pipeline = MarkdownPipeline ?? ModelBuilder.DefaultPipeline;
        }

        var builder = new MarkdownPipelineBuilder();
        foreach (var markdigExtension in (MarkdownPipeline ?? ModelBuilder.DefaultPipeline).Extensions)
        {
            builder.Extensions.Add(markdigExtension);
        }

        foreach (var extension in extensions)
        {
            extension.SetupPipeline(builder);
        }

        return _pipeline = builder.Build();
    }

    private void Rebuild()
    {
        ItemsSource = StyleProvider is null
            ? Array.Empty<BlockVm>()
            : ModelBuilder.Parse(Markdown ?? string.Empty, GetPipeline(), ActiveExtensions);
    }
}
