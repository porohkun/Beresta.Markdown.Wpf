namespace Beresta.Markdown.Wpf.Selection;

/// <summary>Где конец отслеживаемого куска стоит в новом тексте своего блока.</summary>
internal static class TextTracking
{
    /// <summary>Сколько знаков текста блока запоминается по обе стороны конца.</summary>
    public const int Context = 24;

    /// <summary>
    /// Позиция конца в тексте блока — от его начала: по тексту с обеих сторон, затем по своему тексту
    /// куска, затем по соседнему; ничего не нашлось — <c>null</c>. Из нескольких вхождений берётся
    /// ближайшее к прежнему месту.
    /// </summary>
    public static int? Place(string text, MdTextRange block, TrackedEnd end)
    {
        var (own, ownShift, other, otherShift) = end.IsStart
            ? (end.After, 0, end.Before, end.Before.Length)
            : (end.Before, end.Before.Length, end.After, 0);

        return Nearest(text, block, end.Before + end.After, end.Before.Length, end.Offset)
               ?? Nearest(text, block, own, ownShift, end.Offset)
               ?? Nearest(text, block, other, otherShift, end.Offset);
    }

    /// <summary>Вхождение <paramref name="needle"/> в блоке, позиция которого со сдвигом ближе всех к <paramref name="near"/>.</summary>
    private static int? Nearest(string text, MdTextRange block, string needle, int shift, int near)
    {
        if (needle.Length == 0 || needle.Length > block.Length)
        {
            return null;
        }

        int? best = null;
        var from = block.Start;
        while (from <= block.End - needle.Length)
        {
            var index = text.IndexOf(needle, from, block.End - from, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            var position = index - block.Start + shift;
            if (best is null || Math.Abs(position - near) < Math.Abs(best.Value - near))
            {
                best = position;
            }

            from = index + 1;
        }

        return best;
    }
}
