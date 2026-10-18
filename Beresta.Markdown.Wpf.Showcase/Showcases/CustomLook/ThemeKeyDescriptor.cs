namespace Beresta.Markdown.Wpf.Showcase.Showcases.CustomLook;

using System.ComponentModel;

/// <summary>Ключ темы вьюера свойством <see cref="ThemeKeys"/>: подпись — сам ключ, категория — группа значений.</summary>
public sealed class ThemeKeyDescriptor : PropertyDescriptor
{
    private readonly Type _type;

    public ThemeKeyDescriptor(string key, Type type, string group)
        : base(NameOf(key), [new DisplayNameAttribute(key), new CategoryAttribute(group)])
    {
        Key = key;
        _type = type;
    }

    public string Key { get; }

    public override Type ComponentType => typeof(ThemeKeys);

    public override bool IsReadOnly => false;

    public override Type PropertyType => _type;

    /// <summary>Имя свойства ключа.</summary>
    // BYDESIGN: редактор привязывается путём из имени свойства, и точка ключа разделила бы его на вложенные свойства.
    public static string NameOf(string key) => key.Replace('.', '_');

    public override bool CanResetValue(object component) => false;

    public override object? GetValue(object? component) => (component as ThemeKeys)?.Get(Key);

    public override void ResetValue(object component)
    {
    }

    public override void SetValue(object? component, object? value) => (component as ThemeKeys)?.Set(Key, value);

    public override bool ShouldSerializeValue(object component) => false;
}
