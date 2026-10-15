namespace MarkdigWpfViewer.Showcase.Diff;

/// <summary>Построчный дифф: совпавшие строки двух текстов по наибольшей общей подпоследовательности.</summary>
public static class LineDiff
{
    /// <summary>Пары номеров совпавших строк, возрастающие в обоих текстах.</summary>
    public static IReadOnlyList<(int Left, int Right)> Match(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        var lengths = new int[left.Count + 1, right.Count + 1];
        for (var i = left.Count - 1; i >= 0; i--)
        {
            for (var j = right.Count - 1; j >= 0; j--)
            {
                lengths[i, j] = left[i] == right[j]
                    ? lengths[i + 1, j + 1] + 1
                    : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
            }
        }

        var matches = new List<(int, int)>();
        for (int i = 0, j = 0; i < left.Count && j < right.Count;)
        {
            if (left[i] == right[j])
            {
                matches.Add((i++, j++));
            }
            else if (lengths[i + 1, j] >= lengths[i, j + 1])
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return matches;
    }

    /// <summary>Сплошные куски строк, которых нет среди совпавших.</summary>
    public static IEnumerable<(int Start, int End)> Unmatched(int count, IEnumerable<int> matched)
    {
        var set = matched.ToHashSet();
        var start = -1;
        for (var line = 0; line <= count; line++)
        {
            var changed = line < count && !set.Contains(line);
            if (changed && start < 0)
            {
                start = line;
            }
            else if (!changed && start >= 0)
            {
                yield return (start, line - 1);
                start = -1;
            }
        }
    }
}
