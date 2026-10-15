namespace MarkdigWpfViewer.Presenters;

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ViewModels;

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

    private static readonly string[] ExternalSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto];

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

    private static void OnAnyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdTextBlock)d).Rebuild();

    private static Span Wrap(Inline inner, Action<Span> configure)
    {
        var span = new Span();
        configure(span);
        span.Inlines.Add(inner);
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

        foreach (var run in Runs)
        {
            AddRun(run);
        }
    }

    private void AddRun(InlineVm inline)
    {
        if (inline.LineBreak)
        {
            Inlines.Add(new LineBreak());
            return;
        }

        if (inline.Code)
        {
            Inlines.Add(CreateInlineCode(inline.Text));
            return;
        }

        Inline current = new Run(inline.Text ?? string.Empty);

        if (inline.Strike)
        {
            current = Wrap(current, span => span.TextDecorations = System.Windows.TextDecorations.Strikethrough);
        }

        if (inline.Italic)
        {
            current = Wrap(current, span => span.FontStyle = FontStyles.Italic);
        }

        if (inline.Bold)
        {
            current = Wrap(current, span => span.FontWeight = FontWeights.SemiBold);
        }

        if (inline.Hyperlink is not null)
        {
            var link = new Hyperlink
            {
                NavigateUri = inline.Hyperlink,
                Foreground = LinkForeground
            };

            // Подписка на каждую ссылку отдельно, а не одна на весь блок: исходный адрес
            // (inline.Href) в RequestNavigateEventArgs не поедет, а именно он нужен обработчику
            // своих ссылок — Uri их нормализует.
            var href = inline.Href ?? inline.Hyperlink.ToString();
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
}