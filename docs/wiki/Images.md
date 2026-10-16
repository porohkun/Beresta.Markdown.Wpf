**English** | [Русский](Images-ru)

Images are drawn inside the text and load in the background. While an image loads, and if it fails,
its alt text is shown in its place.

## Syntax and size

**Any `.md` file**:

```markdown
![Logo](images/logo.png "Tooltip of the image")

![A narrow logo](images/logo.png){width=120}

The logo in a line of text: <img src="images/logo.png" alt="Logo" width="120" height="40">

[![A logo that is a link](images/logo.png)](https://github.com/porohkun/Markdig.Wpf.Viewer)
```

Without a size an image takes its size in pixels, like in a browser, and shrinks to the width of the
text if it is wider. A size in attributes, `{width=120 height=40}`, or in HTML is in pixels, `px`
allowed; with only one side given, the other keeps the proportions. The title is a tooltip, and a
linked image opens its link like a text link does.

An `<img>` tag on its own line, `<a href><img></a>` and `<picture>` are images too, and
`<p align="center">` centers them — see
[Supported Markdown → HTML from READMEs](Supported-Markdown#html-from-readmes).

## Where images come from

The default loader reads:

- `http://` and `https://` addresses;
- files: absolute paths and `file://` addresses;
- WPF resources: `pack://application:,,,/YourApp;component/images/logo.png`;
- `data:image/png;base64,…` addresses.

A relative address is resolved against `BaseUri`, the address of the document. For a document opened
from a file, it is the file itself.

**`MainWindow.xaml.cs`**, opening a file (`using System.IO;`):

```csharp
private void Open(string path)
{
    Viewer.BaseUri = new Uri(Path.GetFullPath(path));
    Viewer.Markdown = File.ReadAllText(path);
}
```

Images are decoded by WPF: PNG, JPEG, BMP, GIF (without animation), TIFF, ICO. A format WPF cannot
decode, such as SVG, shows the alt text — or plug in a loader that can. The last 64 loaded images are
kept, so a block that scrolls back into view or survives a new text shows its image at once.

## Your own loader

`IMdImageLoader` has one method: by the address from the Markdown (`url`) and the same address
resolved against `BaseUri` (`uri`), it returns a frozen `ImageSource`, or `null` to show the alt text.
A loader of your own serves images from a database, an archive, an authorized server, or adds formats.

Pictures by name from the `attachments` folder of the application: `![Chart](attachment:chart.png)`.

**`AttachmentImageLoader.cs`**:

```csharp
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MarkdigWpfViewer;

public sealed class AttachmentImageLoader : IMdImageLoader
{
    private const string Scheme = "attachment:";

    public async Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken)
    {
        if (!url.StartsWith(Scheme, StringComparison.Ordinal))
        {
            return await MdImageLoader.Instance.LoadAsync(url, uri, cancellationToken);
        }

        var path = Path.Combine(AppContext.BaseDirectory, "attachments", Path.GetFileName(url[Scheme.Length..]));
        if (!File.Exists(path))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        return await Task.Run(() =>
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream(bytes);
            image.EndInit();
            image.Freeze();
            return (ImageSource)image;
        }, cancellationToken);
    }
}
```

**`MainWindow.xaml.cs`**, in the constructor after `InitializeComponent()`:

```csharp
Viewer.ImageLoader = new AttachmentImageLoader();
```

`ImageLoader="{x:Null}"` turns images off: every image shows its alt text.

## The look

The alt text color is the key `Md.Image.AltForeground`, the whole control is the style
`Md.Image.Style`, and the `ImageTemplate` of a style provider replaces it altogether — see
[Styles and Templates](Styles-and-Templates). Screen readers name an image by its alt text — see
[Accessibility](Accessibility).
