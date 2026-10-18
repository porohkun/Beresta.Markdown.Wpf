namespace Beresta.Markdown.Wpf.Extensions;

using System.Collections.ObjectModel;

/// <summary>
/// Extensions of a viewer; the order is the order in which they are asked when parsing and choosing
/// a template.
/// </summary>
public sealed class MdExtensionCollection : ObservableCollection<MdExtension>
{
}