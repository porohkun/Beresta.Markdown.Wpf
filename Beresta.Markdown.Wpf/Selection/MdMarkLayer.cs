namespace Beresta.Markdown.Wpf.Selection;

/// <summary>
/// Пометки одного владельца: поиска, диффа, комментариев приложения. Слои рисуются в порядке
/// <see cref="MarkdownViewer.MarkLayers"/>, выделение — поверх всех.
/// </summary>
public sealed class MdMarkLayer
{
    private IReadOnlyList<MdMark> _marks = [];

    public MdMarkLayer(string name)
    {
        Name = name;
    }

    /// <summary>Пометки слоя сменились: их заменило приложение или передвинул новый текст.</summary>
    public event EventHandler? Changed;

    public string Name { get; }

    /// <summary>Пометки слоя; заменяются целиком.</summary>
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
    /// Пометки следуют за новым текстом, как выделение; пропавшие убираются. Без этого новый текст
    /// пометок не трогает — их пересчитывает владелец.
    /// </summary>
    public bool FollowsText { get; set; }
}
