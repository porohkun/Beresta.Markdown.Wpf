namespace MarkdigWpfViewer.Showcase.Showcases.CustomLook;

using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

/// <summary>Свойства <see cref="ThemeKeys"/>: по свойству на цвет, размер и отступ темы вьюера, по группе на блок.</summary>
public sealed class ThemeKeysProvider : TypeDescriptionProvider
{
    private const string Common = "Общие";

    private static readonly Uri ThemeSource = new("pack://application:,,,/Markdig.Wpf.Viewer;component/Themes/Generic.xaml");

    private static readonly Lazy<PropertyDescriptorCollection> Keys = new(Collect);

    /// <summary>Группа ключа по блоку из его имени <c>Md.Блок.Свойство</c>; сквозные ключи — в общей.</summary>
    private static readonly Dictionary<string, string> Groups = new(StringComparer.Ordinal)
    {
        ["Paragraph"] = "Текст",
        ["Abbreviation"] = "Текст",
        ["Marked"] = "Текст",
        ["Heading"] = "Заголовки",
        ["InlineCode"] = "Код в строке",
        ["Kbd"] = "Клавиши",
        ["CodeBlock"] = "Блок кода",
        ["Quote"] = "Цитата",
        ["Callout"] = "Плашки",
        ["Details"] = "Сворачиваемый блок",
        ["List"] = "Списки",
        ["Task"] = "Задачи",
        ["DefinitionList"] = "Список определений",
        ["Table"] = "Таблица",
        ["Figure"] = "Фигура",
        ["Footer"] = "Подвал",
        ["Footnote"] = "Сноски",
        ["FootnoteTip"] = "Сноски",
        ["HorizontalRule"] = "Разделитель",
        ["Image"] = "Картинки",
        ["Gutter"] = "Поля с номерами строк",
        ["Mark"] = "Пометки",
        ["Mention"] = "Упоминания",
        ["ColorCode"] = "Коды цветов",
        ["Critic"] = "Правки CriticMarkup",
        ["Spoiler"] = "Спойлеры",
        ["LinkCard"] = "Карточки-ссылки",
        ["VideoCard"] = "Карточки видео",
        ["Toc"] = "Оглавление"
    };

    public ThemeKeysProvider()
        : base(TypeDescriptor.GetProvider(typeof(object)))
    {
    }

    public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
        => new KeysDescriptor(base.GetTypeDescriptor(objectType, instance));

    /// <summary>Ключи темы вьюера из ресурсов приложения, по имени.</summary>
    private static PropertyDescriptorCollection Collect()
    {
        var theme = Application.Current?.Resources.MergedDictionaries.FirstOrDefault(dictionary => dictionary.Source == ThemeSource);
        if (theme is null)
        {
            return PropertyDescriptorCollection.Empty;
        }

        var keys = theme.Keys
            .OfType<string>()
            .Where(key => key.StartsWith("Md.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(key => (Key: key, Type: theme[key]?.GetType()))
            .Where(entry => entry.Type is not null && IsEditable(entry.Type))
            .Select(entry => (PropertyDescriptor)new ThemeKeyDescriptor(entry.Key, entry.Type!, GroupOf(entry.Key)))
            .ToArray();
        return new(keys, readOnly: true);
    }

    /// <summary>Значение, которое правит панель: цвет, размер, отступ.</summary>
    private static bool IsEditable(Type type)
        => type == typeof(SolidColorBrush)
           || type == typeof(Thickness)
           || type == typeof(double)
           || type == typeof(CornerRadius)
           || type == typeof(GridLength);

    /// <summary>Группа ключа по блоку из его имени; у заголовков уровень не в счёт.</summary>
    private static string GroupOf(string key)
    {
        var block = key.Split('.')[1].TrimEnd('1', '2', '3', '4', '5', '6');
        return Groups.GetValueOrDefault(block, Common);
    }

    /// <summary>Описание типа со свойствами-ключами.</summary>
    private sealed class KeysDescriptor : CustomTypeDescriptor
    {
        public KeysDescriptor(ICustomTypeDescriptor? parent)
            : base(parent)
        {
        }

        public override PropertyDescriptorCollection GetProperties() => Keys.Value;

        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes) => Keys.Value;
    }
}
