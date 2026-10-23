namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Controls;
using Kit;

/// <summary>Шаблон значения карточки свойств по его типу: <c>Md.Properties.&lt;Тип&gt;.Template</c>, иначе — текстом.</summary>
internal sealed class PropertyTemplates : DataTemplateSelector
{
    public static PropertyTemplates Instance { get; } = new();

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => container is FrameworkElement element
            ? (item is PropertyVm { Type.Name: var name } ? element.TryFindResource($"Md.Properties.{name}.Template") : null) as DataTemplate
              ?? element.TryFindResource("Md.Properties.Text.Template") as DataTemplate
            : null;
}
