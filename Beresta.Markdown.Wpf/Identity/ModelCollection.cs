namespace Beresta.Markdown.Wpf.Identity;

using System.Collections.ObjectModel;
using ViewModels;

/// <summary>
/// Список элементов, который меняется правками, а не подменой: элемент с прежним идентификатором
/// заменяется на месте, и его контейнер переживает новый текст.
/// </summary>
internal sealed class ModelCollection<T> : ObservableCollection<T>
    where T : NodeVm
{
    public void Apply(IReadOnlyList<T> next)
    {
        var kept = next.Select(KeyOf).ToHashSet();
        for (var i = Count - 1; i >= 0; i--)
        {
            if (!kept.Contains(KeyOf(this[i])))
            {
                RemoveAt(i);
            }
        }

        for (var index = 0; index < next.Count; index++)
        {
            var item = next[index];
            var key = KeyOf(item);

            if (index < Count && Equals(KeyOf(this[index]), key))
            {
                if (!ReferenceEquals(this[index], item))
                {
                    this[index] = item;
                }

                continue;
            }

            var moved = FindKey(key, index + 1);
            if (moved < 0)
            {
                Insert(index, item);
                continue;
            }

            Move(moved, index);
            if (!ReferenceEquals(this[index], item))
            {
                this[index] = item;
            }
        }

        while (Count > next.Count)
        {
            RemoveAt(Count - 1);
        }
    }

    private static object KeyOf(T item) => item.NodeId != 0 ? item.NodeId : item;

    private int FindKey(object key, int start)
    {
        for (var i = start; i < Count; i++)
        {
            if (Equals(KeyOf(this[i]), key))
            {
                return i;
            }
        }

        return -1;
    }
}