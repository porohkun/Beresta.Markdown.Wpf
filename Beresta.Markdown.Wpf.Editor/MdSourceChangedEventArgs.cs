namespace Beresta.Markdown.Wpf.Editor;

/// <summary>The source in the editor changed: typing, an undo, a control, a new text.</summary>
public sealed class MdSourceChangedEventArgs : EventArgs
{
    /// <summary>The source changed by <paramref name="change"/>.</summary>
    public MdSourceChangedEventArgs(MdSourceEdit change)
    {
        Change = change;
    }

    /// <summary>What changed: the place, how many characters went away and what came in their place.</summary>
    public MdSourceEdit Change { get; }
}
