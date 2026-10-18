namespace Beresta.Markdown.Wpf.Selection;

/// <summary>
/// A widget that gives its text and nested blocks to the text map. A widget without it is
/// indivisible: it is selected and copied as a whole and has no text in the map.
/// </summary>
public interface IMdTextSource
{
    /// <summary>Writes the text and the nested blocks of the widget to the text map in reading order.</summary>
    void WriteText(MdTextWriter writer);
}