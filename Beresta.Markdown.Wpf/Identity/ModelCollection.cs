namespace Beresta.Markdown.Wpf.Identity;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using ViewModels;

/// <summary>
/// Список элементов, который меняется правками, а не подменой: элемент с прежним идентификатором
/// заменяется на месте, и его контейнер переживает новый текст. Когда общих элементов меньше
/// половины большего из списков, содержимое заменяется целиком одним уведомлением.
/// </summary>
internal sealed class ModelCollection<T> : ObservableCollection<T>
    where T : NodeVm
{
    public void Apply(IReadOnlyList<T> next)
    {
        var kept = next.Select(KeyOf).ToHashSet();
        var common = this.Count(item => kept.Contains(KeyOf(item)));
        if (2 * common < Math.Max(Count, next.Count))
        {
            Replace(next);
            return;
        }

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

    /// <summary>Заменяет содержимое целиком одним уведомлением <see cref="NotifyCollectionChangedAction.Reset"/>.</summary>
    private void Replace(IReadOnlyList<T> next)
    {
        CheckReentrancy();
        Items.Clear();
        foreach (var item in next)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

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
