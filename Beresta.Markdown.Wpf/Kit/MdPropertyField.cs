namespace Beresta.Markdown.Wpf.Kit;

/// <summary>A property the application knows: its key and the type of its value.</summary>
public sealed class MdPropertyField
{
    /// <summary>The key of the property in front matter.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The type of the value; it is stronger than the guess by the value. None — guessed by the value.</summary>
    public MdPropertyType? Type { get; set; }
}
