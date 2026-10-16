namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Automation;
using ViewModels;

/// <summary>
/// Картинка документа: грузит себя загрузчиком вьюера, пока не загрузилась — показывает alt; не
/// шире текста, в котором стоит. Картинка внутри ссылки открывается как ссылка.
/// </summary>
public sealed class MdImage : Control
{
    public static readonly DependencyProperty ImageProperty =
        DependencyProperty.Register(
            nameof(Image),
            typeof(ImageVm),
            typeof(MdImage),
            new(null, OnImageChanged));

    private static readonly DependencyPropertyKey ImageSourcePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ImageSource),
            typeof(ImageSource),
            typeof(MdImage),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Загруженная картинка; пока её нет — <c>null</c>.</summary>
    public static readonly DependencyProperty ImageSourceProperty = ImageSourcePropertyKey.DependencyProperty;

    private MdTextBlock? _text;
    private bool _pressed;

    public MdImage()
    {
        Loaded += (_, _) =>
        {
            Watch(FindText());
            Load();
            InvalidateMeasure();
        };
        Unloaded += (_, _) => Watch(null);
    }

    public ImageVm? Image
    {
        get => (ImageVm?)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public ImageSource? ImageSource => (ImageSource?)GetValue(ImageSourceProperty);

    protected override Size MeasureOverride(Size constraint)
    {
        if (ImageSource is not { } source || Image is not { } image)
        {
            return base.MeasureOverride(constraint);
        }

        var natural = NaturalSize(source, image);
        var limit = Math.Min(constraint.Width, FindText() is { ActualWidth: > 0 } text ? text.ActualWidth : double.PositiveInfinity);
        var scale = natural.Width > limit ? limit / natural.Width : 1;
        var size = new Size(natural.Width * scale, natural.Height * scale);

        base.MeasureOverride(size);
        return size;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MdImageAutomationPeer(this);

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _pressed = Image?.Href is not null;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_pressed && Image is { Href: { } href } image)
        {
            _pressed = false;
            e.Handled = true;
            MarkdownLink.Activate(this, href, image.Hyperlink);
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _pressed = false;
    }

    private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (MdImage)d;
        var image = e.NewValue as ImageVm;
        view.Cursor = image?.Href is null ? null : Cursors.Hand;
        view.ToolTip = image?.Title;
        view.Load();
    }

    /// <summary>Размер из атрибутов, а без них — в пикселях картинки, как в браузере.</summary>
    private static Size NaturalSize(ImageSource source, ImageVm image)
    {
        var (width, height) = source is BitmapSource bitmap
            ? (bitmap.PixelWidth, bitmap.PixelHeight)
            : (source.Width, source.Height);

        return (image.Width, image.Height) switch
        {
            ({ } w, { } h) => new(w, h),
            ({ } w, null) => new(w, width > 0 ? w * height / width : 0),
            (null, { } h) => new(height > 0 ? h * width / height : 0, h),
            _ => new(width, height)
        };
    }

    /// <summary>Адрес из разметки, разобранный от адреса документа.</summary>
    private static Uri? Resolve(string url, Uri? baseUri)
    {
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        if (baseUri is { IsAbsoluteUri: true } && Uri.TryCreate(baseUri, url, out var combined))
        {
            return combined;
        }

        return Uri.TryCreate(url, UriKind.Relative, out var relative) ? relative : null;
    }

    private MdTextBlock? FindText()
    {
        for (var node = VisualTreeHelper.GetParent(this); node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is MdTextBlock text)
            {
                return text;
            }
        }

        return null;
    }

    /// <summary>Следит за шириной текста вокруг: картинка не шире него.</summary>
    private void Watch(MdTextBlock? text)
    {
        if (_text is not null)
        {
            _text.SizeChanged -= OnTextSizeChanged;
        }

        _text = text;
        if (_text is not null)
        {
            _text.SizeChanged += OnTextSizeChanged;
        }
    }

    private void OnTextSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged)
        {
            InvalidateMeasure();
        }
    }

    /// <summary>Загруженное — сразу, загружаемое — когда придёт; сбой загрузчика оставляет alt.</summary>
    private void Load()
    {
        if (Image is not { } image || MarkdownViewer.Owning(this) is not { ImageLoader: { } loader } viewer)
        {
            SetValue(ImageSourcePropertyKey, null);
            return;
        }

        var uri = Resolve(image.Url, viewer.BaseUri);
        var loading = MdImageCache.Get(
            loader,
            uri?.ToString() ?? image.Url,
            () => loader.LoadAsync(image.Url, uri, CancellationToken.None));

        if (loading.IsCompleted)
        {
            Show(image, loading);
            return;
        }

        SetValue(ImageSourcePropertyKey, null);
        loading.ContinueWith(done => Dispatcher.BeginInvoke(() => Show(image, done)), TaskScheduler.Default);
    }

    private void Show(ImageVm image, Task<ImageSource?> loading)
    {
        if (ReferenceEquals(Image, image))
        {
            SetValue(ImageSourcePropertyKey, loading.IsCompletedSuccessfully ? loading.Result : null);
        }
    }
}
