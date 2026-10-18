namespace Beresta.Markdown.Wpf;

/// <summary>Сколько раз знак встречается в куске текста.</summary>
internal static class CharCount
{
    public static int In(ReadOnlySpan<char> text, char symbol)
    {
        var count = 0;
        foreach (var current in text)
        {
            if (current == symbol)
            {
                count++;
            }
        }

        return count;
    }
}
