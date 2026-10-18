namespace MarkdigWpfViewer.Showcase.Localization;

using System.Windows.Data;
using System.Windows.Markup;

/// <summary>Строка витрины в разметке, <c>{loc:Text Ключ}</c>: меняется вместе с языком, работает и в шаблонах.</summary>
public sealed class TextExtension : MarkupExtension
{
    public TextExtension()
    {
    }

    public TextExtension(string key)
    {
        Key = key;
    }

    /// <summary>Ключ строки в <c>Strings.resx</c>.</summary>
    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
        => new Binding($"[{Key}]") { Source = LocalizedStrings.Instance, Mode = BindingMode.OneWay }.ProvideValue(serviceProvider);
}
