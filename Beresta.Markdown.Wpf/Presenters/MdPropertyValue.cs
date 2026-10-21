namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows.Controls;
using Kit;

/// <summary>
/// The value of a row of the properties card: drawn with the theme template of its type,
/// <c>Md.Properties.&lt;Type&gt;.Template</c>; a type without a template — with <c>Md.Properties.Text.Template</c>.
/// </summary>
public sealed class MdPropertyValue : ContentControl
{
    /// <summary>Creates a value; its row is the <see cref="PropertyVm"/> in <see cref="ContentControl.Content"/>.</summary>
    public MdPropertyValue()
    {
        Focusable = false;
        IsTabStop = false;
        ContentTemplateSelector = PropertyTemplates.Instance;
    }
}
