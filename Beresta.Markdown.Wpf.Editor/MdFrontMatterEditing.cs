namespace Beresta.Markdown.Wpf.Editor;

/// <summary>How the editor edits front matter.</summary>
public enum MdFrontMatterEditing
{
    /// <summary>Not edited: the properties card only shows, and the edited source starts after front matter.</summary>
    None,

    /// <summary>In the input fields of the properties card; the edited source starts after front matter.</summary>
    Fields,

    /// <summary>
    /// As text: in <see cref="MdEditMode.Blocks"/> and <see cref="MdEditMode.Full"/> front matter is
    /// edited like any other block, in <see cref="MdEditMode.Controls"/> — in the fields of the card.
    /// </summary>
    Text
}
