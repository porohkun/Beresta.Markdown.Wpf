namespace Beresta.Markdown.Wpf.Kit;

using System.ComponentModel;
using System.Globalization;

/// <summary>Тип значения из XAML по имени: свои типы карточки — без учёта регистра, другие — как написаны.</summary>
internal sealed class MdPropertyTypeConverter : TypeConverter
{
    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => value is string name
            ? name.Trim().ToLowerInvariant() switch
            {
                "text" => MdPropertyType.Text,
                "bool" => MdPropertyType.Bool,
                "date" => MdPropertyType.Date,
                "list" => MdPropertyType.List,
                _ => new MdPropertyType(name.Trim())
            }
            : base.ConvertFrom(context, culture, value);
}
