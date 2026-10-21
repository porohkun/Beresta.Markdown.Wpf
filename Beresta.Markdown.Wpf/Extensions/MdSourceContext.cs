namespace Beresta.Markdown.Wpf.Extensions;

/// <summary>The source a widget writes into: the text the shown document was built from.</summary>
public sealed class MdSourceContext
{
    internal MdSourceContext(string text)
    {
        Text = text;
    }

    /// <summary>The text of the shown document; the positions of the model point into it.</summary>
    public string Text { get; }
}
