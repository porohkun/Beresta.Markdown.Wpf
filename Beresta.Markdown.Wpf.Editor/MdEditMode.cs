namespace Beresta.Markdown.Wpf.Editor;

/// <summary>How the editor edits its document.</summary>
public enum MdEditMode
{
    /// <summary>Only shows, like the viewer: task boxes and folding do not touch the source.</summary>
    View,

    /// <summary>Task boxes and folding write into the source; the text itself is not typed.</summary>
    Controls,

    /// <summary>The blocks under the caret show their source and are typed in place; the rest stays drawn.</summary>
    Blocks,

    /// <summary>The whole source is typed in place of the drawn document.</summary>
    Full
}
