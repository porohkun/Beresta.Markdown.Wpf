namespace Beresta.Markdown.Wpf.Editor;

using ICSharpCode.AvalonEdit.Document;

/// <summary>
/// Тексты, которые редактор отдал вьюеру или получил от приложения, с версиями своего документа на
/// тот момент: место в тексте, который показывает вьюер, переводится на нынешний документ.
/// </summary>
internal sealed class SourceVersions
{
    private const int Capacity = 32;

    private readonly List<(string Text, ITextSourceVersion Version)> _known = [];

    public void Remember(string text, ITextSourceVersion version)
    {
        _known.RemoveAll(entry => ReferenceEquals(entry.Text, text));
        _known.Add((text, version));
        if (_known.Count > Capacity)
        {
            _known.RemoveAt(0);
        }
    }

    /// <summary>
    /// Место из текста <paramref name="text"/> в нынешнем документе; текст незнаком и с документом
    /// не совпадает — <c>null</c>.
    /// </summary>
    public int? Map(string text, int offset, TextDocument document, AnchorMovementType movement)
    {
        for (var i = _known.Count - 1; i >= 0; i--)
        {
            var (known, version) = _known[i];
            if (ReferenceEquals(known, text) && version.BelongsToSameDocumentAs(document.Version))
            {
                return version.MoveOffsetTo(document.Version, offset, movement);
            }
        }

        return string.Equals(text, document.Text, StringComparison.Ordinal) ? offset : null;
    }

    /// <summary>
    /// Место нынешнего документа в тексте <paramref name="text"/>; текст незнаком — то же место,
    /// прижатое к длине текста.
    /// </summary>
    public int ToText(string text, int offset, TextDocument document, AnchorMovementType movement)
    {
        for (var i = _known.Count - 1; i >= 0; i--)
        {
            var (known, version) = _known[i];
            if (ReferenceEquals(known, text) && version.BelongsToSameDocumentAs(document.Version))
            {
                return Math.Clamp(document.Version.MoveOffsetTo(version, offset, movement), 0, text.Length);
            }
        }

        return Math.Clamp(offset, 0, text.Length);
    }
}
