namespace Beresta.Markdown.Wpf.Extensions;

/// <summary>The state of a styled piece of text that a click on the piece changes.</summary>
public interface IMdClickableState
{
    /// <summary>The piece was clicked without selecting text.</summary>
    void OnClick();
}
