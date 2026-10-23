namespace Beresta.Markdown.Wpf.Kit;

using System.ComponentModel;

/// <summary>
/// The type of a value of front matter: how the properties card shows and edits it. The type <c>X</c> is
/// drawn with the theme template <c>Md.Properties.X.Template</c>; a type without a template — as text.
/// </summary>
/// <param name="Name">The name of the type: its theme template is <c>Md.Properties.&lt;Name&gt;.Template</c>.</param>
[TypeConverter(typeof(MdPropertyTypeConverter))]
public sealed record MdPropertyType(string Name)
{
    /// <summary>Text in an input field.</summary>
    public static MdPropertyType Text { get; } = new(nameof(Text));

    /// <summary>A flag: <c>true</c> and <c>false</c>, <c>yes</c> and <c>no</c>, <c>on</c> and <c>off</c>.</summary>
    public static MdPropertyType Bool { get; } = new(nameof(Bool));

    /// <summary>A date <c>yyyy-MM-dd</c>.</summary>
    public static MdPropertyType Date { get; } = new(nameof(Date));

    /// <summary>A list <c>[a, b]</c> or <c>- a</c> lines.</summary>
    public static MdPropertyType List { get; } = new(nameof(List));

    /// <inheritdoc/>
    public override string ToString() => Name;
}
