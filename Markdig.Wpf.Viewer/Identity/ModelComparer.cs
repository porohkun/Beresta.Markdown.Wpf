namespace MarkdigWpfViewer.Identity;

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using ViewModels;

/// <summary>Равенство вьюмоделей по содержимому вместе со всем вложенным.</summary>
internal static class ModelComparer
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Properties = new();

    public static bool Equal(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (left is IEnumerable leftItems and not string && right is IEnumerable rightItems and not string)
        {
            return SequenceEqual(leftItems, rightItems);
        }

        if (left.GetType() != right.GetType())
        {
            return false;
        }

        return left is NodeVm
            ? PropertiesOf(left.GetType()).All(property => Equal(property.GetValue(left), property.GetValue(right)))
            : left.Equals(right);
    }

    private static bool SequenceEqual(IEnumerable left, IEnumerable right)
    {
        var leftItems = left.GetEnumerator();
        var rightItems = right.GetEnumerator();

        while (true)
        {
            var hasLeft = leftItems.MoveNext();
            if (hasLeft != rightItems.MoveNext())
            {
                return false;
            }

            if (!hasLeft)
            {
                return true;
            }

            if (!Equal(leftItems.Current, rightItems.Current))
            {
                return false;
            }
        }
    }

    private static PropertyInfo[] PropertiesOf(Type type)
    {
        return Properties.GetOrAdd(
            type,
            static type => type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .ToArray());
    }
}
