namespace Beresta.Markdown.Wpf.Selection;

/// <summary>
/// Marks of one owner: search, a diff, comments of the application. Layers are drawn in the order
/// of <see cref="MarkdownViewer.MarkLayers"/>, the selection — over all of them.
/// </summary>
public sealed class MdMarkLayer
{
    private IReadOnlyList<MdMark> _marks = [];

    /// <summary>An empty layer named <paramref name="name"/>.</summary>
    public MdMarkLayer(string name)
    {
        Name = name;
    }

    /// <summary>The marks of the layer changed: the application replaced them or new text moved them.</summary>
    public event EventHandler? Changed;

    /// <summary>The name of the layer.</summary>
    public string Name { get; }

    /// <summary>The marks of the layer; replaced as a whole.</summary>
    public IReadOnlyList<MdMark> Marks
    {
        get => _marks;
        set
        {
            _marks = value ?? [];
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Marks follow new text, like the selection; the ones that vanished are removed. Without this
    /// new text does not touch the marks — their owner recomputes them.
    /// </summary>
    public bool FollowsText { get; set; }
}
