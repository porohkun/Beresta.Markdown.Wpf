[English](Images) | **Русский**

Картинки рисуются внутри текста и грузятся в фоне. Пока картинка грузится и если загрузка не
удалась, на её месте виден её alt.

## Синтаксис и размер

**Любой `.md`-файл**:

```markdown
![Logo](images/logo.png "Tooltip of the image")

![A narrow logo](images/logo.png){width=120}

The logo in a line of text: <img src="images/logo.png" alt="Logo" width="120" height="40">

[![A logo that is a link](images/logo.png)](https://github.com/porohkun/Markdig.Wpf.Viewer)
```

Без размера картинка берёт свой размер в пикселях, как в браузере, и сжимается до ширины текста,
если шире него. Размер в атрибутах, `{width=120 height=40}`, или в HTML — в пикселях, можно с `px`;
если задана одна сторона, вторая сохраняет пропорции. Заголовок — подсказка, а картинка-ссылка
открывает свою ссылку, как текстовая.

Тег `<img>` один на строке — для CommonMark это HTML-блок, и он показывается текстом; ставь его
внутрь строки текста.

## Откуда берутся картинки

Стандартный загрузчик картинок читает:

- адреса `http://` и `https://`;
- файлы: абсолютные пути и адреса `file://`;
- ресурсы WPF: `pack://application:,,,/YourApp;component/images/logo.png`;
- адреса `data:image/png;base64,…`.

Относительный адрес разбирается от `BaseUri` — адреса документа. Для документа, открытого из файла,
это сам файл.

**`MainWindow.xaml.cs`**, открытие файла (`using System.IO;`):

```csharp
private void Open(string path)
{
    Viewer.BaseUri = new Uri(Path.GetFullPath(path));
    Viewer.Markdown = File.ReadAllText(path);
}
```

Картинки декодирует WPF: PNG, JPEG, BMP, GIF (без анимации), TIFF, ICO. Формат, который WPF не
декодирует, например SVG, показывает alt — или нужен загрузчик, который его умеет. Последние 64
загруженные картинки запоминаются, поэтому блок, вернувшийся в видимое или переживший новый текст,
показывает картинку сразу.

## Свой загрузчик

У `IMdImageLoader` один метод: по адресу из разметки (`url`) и тому же адресу, разобранному от
`BaseUri` (`uri`), он возвращает замороженный `ImageSource` или `null`, чтобы показать alt. Свой
загрузчик берёт картинки из базы, архива, с сервера с авторизацией или добавляет форматы.

Картинки по имени из папки `attachments` приложения: `![Chart](attachment:chart.png)`.

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

**`MainWindow.xaml.cs`**, в конструкторе после `InitializeComponent()`:

```csharp
Viewer.ImageLoader = new AttachmentImageLoader();
```

`ImageLoader="{x:Null}"` выключает картинки: каждая показывает свой alt.

## Вид

Цвет alt — ключ `Md.Image.AltForeground`, весь контрол — стиль `Md.Image.Style`, а `ImageTemplate`
провайдера стилей заменяет его совсем — см. [Стили и шаблоны](Styles-and-Templates-ru). Экранные
дикторы называют картинку её alt — см. [Доступность](Accessibility-ru).
