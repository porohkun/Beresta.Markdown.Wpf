namespace Beresta.Markdown.Wpf.Editor;

using ICSharpCode.AvalonEdit.Document;

/// <summary>
/// Тексты, которые редактор отдал вьюеру или получил от приложения, с версиями своего документа на
/// тот момент: место в тексте, который показывает вьюер, переводится на нынешний документ. Текст
/// помнится, пока его кто-то держит.
/// </summary>
internal sealed class SourceVersions
{
    private readonly List<(WeakReference<string> Text, ITextSourceVersion Version)> _known = [];

    public void Remember(string text, ITextSourceVersion version)
    {
        _known.RemoveAll(entry => !entry.Text.TryGetTarget(out var known) || ReferenceEquals(known, text));
        _known.Add((new(text), version));
    }

    /// <summary>
    /// Место из текста <paramref name="text"/> в нынешнем документе; текст незнаком и с документом
    /// не совпадает — <c>null</c>.
    /// </summary>
    public int? Map(string text, int offset, TextDocument document, AnchorMovementType movement)
    {
        return VersionOf(text, document) is { } version
            ? version.MoveOffsetTo(document.Version, offset, movement)
            : string.Equals(text, document.Text, StringComparison.Ordinal) ? offset : null;
    }

    /// <summary>
    /// Место нынешнего документа в тексте <paramref name="text"/>; текст незнаком — то же место,
    /// прижатое к длине текста.
    /// </summary>
    public int ToText(string text, int offset, TextDocument document, AnchorMovementType movement)
    {
        return VersionOf(text, document) is { } version
            ? Math.Clamp(document.Version.MoveOffsetTo(version, offset, movement), 0, text.Length)
            : Math.Clamp(offset, 0, text.Length);
    }

    /// <summary>Версия документа, с которой запомнен этот самый текст; не запомнен — <c>null</c>.</summary>
    private ITextSourceVersion? VersionOf(string text, TextDocument document)
    {
        for (var i = _known.Count - 1; i >= 0; i--)
        {
            var (known, version) = _known[i];
            if (known.TryGetTarget(out var entry) && ReferenceEquals(entry, text) && version.BelongsToSameDocumentAs(document.Version))
            {
                return version;
            }
        }

        return null;
    }
}
