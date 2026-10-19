namespace Beresta.Markdown.Wpf;

using Extensions;

/// <summary>An exception the viewer caught while it built or drew its document.</summary>
public sealed class MdRenderFailedEventArgs : EventArgs
{
    /// <summary>A caught exception, the extension whose code threw it and where it was thrown.</summary>
    public MdRenderFailedEventArgs(Exception exception, MdExtension? extension, bool isBackground)
    {
        Exception = exception;
        Extension = extension;
        IsBackground = isBackground;
    }

    /// <summary>The caught exception.</summary>
    public Exception Exception { get; }

    /// <summary>
    /// The extension whose method or widget threw; a set if it was thrown inside the set. <c>null</c> —
    /// Markdig threw.
    /// </summary>
    public MdExtension? Extension { get; }

    /// <summary>
    /// The exception was thrown by a background parse; the same text parsed on the interface thread did
    /// not fail and is shown.
    /// </summary>
    public bool IsBackground { get; }
}
