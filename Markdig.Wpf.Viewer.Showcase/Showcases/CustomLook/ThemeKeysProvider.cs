namespace MarkdigWpfViewer.Showcase.Showcases.CustomLook;

using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Localization;

/// <summary>Свойства <see cref="ThemeKeys"/>: по свойству на цвет, размер и отступ темы вьюера, по группе на блок.</summary>
public sealed class ThemeKeysProvider : TypeDescriptionProvider
{
    private const string Common = "Common";

    private static readonly Uri ThemeSource = new("pack://application:,,,/Markdig.Wpf.Viewer;component/Themes/Generic.xaml");

    /// <summary>Группа ключа по блоку из его имени <c>Md.Блок.Свойство</c>: строка <c>CustomLook.Group.&lt;группа&gt;</c>; сквозные ключи — в общей.</summary>
    private static readonly Dictionary<string, string> Groups = new(StringComparer.Ordinal)
    {
        ["Paragraph"] = "Text",
        ["Abbreviation"] = "Text",
        ["Marked"] = "Text",
        ["Heading"] = "Headings",
        ["InlineCode"] = "InlineCode",
        ["Kbd"] = "Kbd",
        ["CodeBlock"] = "CodeBlock",
        ["Quote"] = "Quote",
        ["Callout"] = "Callout",
        ["Details"] = "Details",
        ["List"] = "List",
        ["Task"] = "Task",
        ["DefinitionList"] = "DefinitionList",
        ["Table"] = "Table",
        ["Figure"] = "Figure",
        ["Footer"] = "Footer",
        ["Footnote"] = "Footnote",
        ["FootnoteTip"] = "Footnote",
        ["HorizontalRule"] = "HorizontalRule",
        ["Image"] = "Image",
        ["Gutter"] = "Gutter",
        ["Mark"] = "Mark",
        ["Mention"] = "Mention",
        ["ColorCode"] = "ColorCode",
        ["Spoiler"] = "Spoiler",
        ["Card"] = "Card",
        ["LinkCard"] = "Card",
        ["VideoCard"] = "Card",
        ["Toc"] = "Toc"
    };

    private static (CultureInfo Culture, PropertyDescriptorCollection Keys)? _cache;

    /// <summary>Ключи темы с группами на текущем языке.</summary>
    private static PropertyDescriptorCollection Keys
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture;
            if (_cache is not { } cache || !Equals(cache.Culture, culture))
            {
                cache = (culture, Collect());
                _cache = cache;
            }

            return cache.Keys;
        }
    }

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

    /// <summary>Название группы ключа по блоку из его имени; у заголовков уровень не в счёт.</summary>
    private static string GroupOf(string key)
    {
        var block = key.Split('.')[1].TrimEnd('1', '2', '3', '4', '5', '6');
        return Localizer.Text($"CustomLook.Group.{Groups.GetValueOrDefault(block, Common)}");
    }

    /// <summary>Описание типа со свойствами-ключами.</summary>
    private sealed class KeysDescriptor : CustomTypeDescriptor
    {
        public KeysDescriptor(ICustomTypeDescriptor? parent)
            : base(parent)
        {
        }

        public override PropertyDescriptorCollection GetProperties() => Keys;

        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes) => Keys;
    }
}
