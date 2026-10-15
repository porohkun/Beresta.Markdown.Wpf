namespace MarkdigWpfViewer.Presenters;

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Styling;
using ViewModels;

/// <summary>Текст блока из инлайнов: начертания, ссылки, код посреди строки, строчные виджеты.</summary>
public class MdTextBlock : TextBlock
{
    public static readonly DependencyProperty RunsProperty =
        DependencyProperty.Register(
            nameof(Runs),
            typeof(IEnumerable<InlineVm>),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    public static readonly DependencyProperty LinkForegroundProperty =
        DependencyProperty.Register(
            nameof(LinkForeground),
            typeof(Brush),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(Brushes.Blue, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    public static readonly DependencyProperty InlineCodeStyleProperty =
        DependencyProperty.Register(
            nameof(InlineCodeStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Стиль жирного (<c>Span</c>).</summary>
    public static readonly DependencyProperty StrongStyleProperty =
        DependencyProperty.Register(
            nameof(StrongStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Стиль курсива (<c>Span</c>).</summary>
    public static readonly DependencyProperty EmphasisStyleProperty =
        DependencyProperty.Register(
            nameof(EmphasisStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Стиль зачёркнутого (<c>Span</c>).</summary>
    public static readonly DependencyProperty StrikeStyleProperty =
        DependencyProperty.Register(
            nameof(StrikeStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    /// <summary>Стиль ссылки (<c>Hyperlink</c>); цвет задаёт <see cref="LinkForeground"/>.</summary>
    public static readonly DependencyProperty LinkStyleProperty =
        DependencyProperty.Register(
            nameof(LinkStyle),
            typeof(Style),
            typeof(MdTextBlock),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    private static readonly string[] ExternalSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto];

    private static readonly DataTemplateSelector SharedSelector = new MarkdownTemplateSelector();

    public MdTextBlock()
    {
        Rebuild();
    }

    public IEnumerable<InlineVm>? Runs
    {
        get => (IEnumerable<InlineVm>?)GetValue(RunsProperty);
        set => SetValue(RunsProperty, value);
    }

    public Brush LinkForeground
    {
        get => (Brush)GetValue(LinkForegroundProperty);
        set => SetValue(LinkForegroundProperty, value);
    }

    public Style InlineCodeStyle
    {
        get => (Style)GetValue(InlineCodeStyleProperty);
        set => SetValue(InlineCodeStyleProperty, value);
    }

    public Style? StrongStyle
    {
        get => (Style?)GetValue(StrongStyleProperty);
        set => SetValue(StrongStyleProperty, value);
    }

    public Style? EmphasisStyle
    {
        get => (Style?)GetValue(EmphasisStyleProperty);
        set => SetValue(EmphasisStyleProperty, value);
    }

    public Style? StrikeStyle
    {
        get => (Style?)GetValue(StrikeStyleProperty);
        set => SetValue(StrikeStyleProperty, value);
    }

    public Style? LinkStyle
    {
        get => (Style?)GetValue(LinkStyleProperty);
        set => SetValue(LinkStyleProperty, value);
    }

    private static void OnAnyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdTextBlock)d).Rebuild();

    private static Span Wrap(Inline inner, Style? style, Action<Span> fallback)
    {
        var span = new Span(inner);
        if (style is null)
        {
            fallback(span);
        }
        else
        {
            span.Style = style;
        }

        return span;
    }

    /// <summary>
    /// Ссылка наружу — системным обработчиком. Открываются только адреса веба и почты;
    /// <c>www.example.com</c> без схемы считается адресом веба.
    /// </summary>
    private static void OpenExternal(string href)
    {
        var target = href.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? "http://" + href : href;
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri) || !ExternalSchemes.Contains(uri.Scheme))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch
        {
            // deliberately ignored
        }
    }

    private void Rebuild()
    {
        Inlines.Clear();

        if (Runs is null)
        {
            return;
        }

        foreach (var inline in Runs)
        {
            switch (inline)
            {
                case LineBreakVm:
                    Inlines.Add(new LineBreak());
                    break;

                case RunVm run:
                    AddRun(run);
                    break;

                default:
                    Inlines.Add(CreateWidget(inline));
                    break;
            }
        }
    }

    private void AddRun(RunVm run)
    {
        if (run.Code)
        {
            Inlines.Add(CreateInlineCode(run.Text));
            return;
        }

        Inline current = new Run(run.Text ?? string.Empty);

        if (run.Strike)
        {
            current = Wrap(current, StrikeStyle, span => span.TextDecorations = System.Windows.TextDecorations.Strikethrough);
        }

        if (run.Italic)
        {
            current = Wrap(current, EmphasisStyle, span => span.FontStyle = FontStyles.Italic);
        }

        if (run.Bold)
        {
            current = Wrap(current, StrongStyle, span => span.FontWeight = FontWeights.SemiBold);
        }

        if (run.Hyperlink is not null)
        {
            var link = new Hyperlink
            {
                NavigateUri = run.Hyperlink,
                Style = LinkStyle,
                Foreground = LinkForeground
            };

            // Подписка на каждую ссылку отдельно, а не одна на весь блок: исходный адрес
            // (inline.Href) в RequestNavigateEventArgs не поедет, а именно он нужен обработчику
            // своих ссылок — Uri их нормализует.
            var href = run.Href ?? run.Hyperlink.ToString();
            link.RequestNavigate += (_, e) =>
            {
                e.Handled = true;
                Navigate(href, e.Uri);
            };

            link.Inlines.Add(current);
            current = link;
        }

        Inlines.Add(current);
    }

    /// <summary>
    /// Сначала спрашиваем приложение (<see cref="MarkdownLink.NavigateEvent"/> всплывает до
    /// любого предка вьюера), и только необработанную ссылку открываем наружу.
    /// </summary>
    private void Navigate(string href, Uri? uri)
    {
        var args = new MarkdownLinkEventArgs(MarkdownLink.NavigateEvent, this, href, uri);
        RaiseEvent(args);

        if (!args.Handled)
            OpenExternal(href);
    }

    private Inline CreateInlineCode(string text)
    {
        var presenter = new InlineCodePresenter
        {
            Text = text,
            Style = InlineCodeStyle
        };

        return new InlineUIContainer(presenter)
        {
            BaselineAlignment = BaselineAlignment.Center
        };
    }

    private Inline CreateWidget(InlineVm inline)
    {
        var presenter = new ContentPresenter
        {
            Content = inline,
            ContentTemplateSelector = TryFindResource("Md.TemplateSelector") as DataTemplateSelector ?? SharedSelector
        };

        return new InlineUIContainer(presenter)
        {
            BaselineAlignment = BaselineAlignment.Center
        };
    }
}
