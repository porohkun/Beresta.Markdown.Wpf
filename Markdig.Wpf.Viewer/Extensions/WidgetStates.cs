namespace MarkdigWpfViewer.Extensions;

using ViewModels;

/// <summary>Состояния виджетов вьюера по устойчивым идентификаторам элементов.</summary>
internal sealed class WidgetStates
{
    private readonly Func<IReadOnlyList<MdExtension>> _extensions;
    private readonly Dictionary<long, (Type Kind, object State)> _states = [];

    public WidgetStates(Func<IReadOnlyList<MdExtension>> extensions)
    {
        _extensions = extensions;
    }

    /// <summary>Состояние виджета; первый раз его создаёт расширение. <c>null</c> — состояния нет.</summary>
    public object? StateOf(object? content)
    {
        if (content is not NodeVm { NodeId: not 0 } node)
        {
            return null;
        }

        if (_states.TryGetValue(node.NodeId, out var known) && known.Kind == node.GetType())
        {
            return known.State;
        }

        foreach (var extension in _extensions())
        {
            if (extension.CreateState(node) is { } state)
            {
                _states[node.NodeId] = (node.GetType(), state);
                return state;
            }
        }

        if (FoldStateOf(node) is { } fold)
        {
            _states[node.NodeId] = (node.GetType(), fold);
            return fold;
        }

        return null;
    }

    /// <summary>Свёрнутость встроенного сворачиваемого блока; у других блоков — <c>null</c>.</summary>
    private static MdFoldState? FoldStateOf(NodeVm node) => node switch
    {
        CalloutVm { IsFoldable: true } callout => new(!callout.IsFolded),
        DetailsVm details => new(details.IsOpen),
        _ => null
    };

    /// <summary>Забывает состояния элементов, которых в документе больше нет.</summary>
    public void Retain(Func<long, bool> exists)
    {
        foreach (var id in _states.Keys.Where(id => !exists(id)).ToList())
        {
            _states.Remove(id);
        }
    }

    public void Clear() => _states.Clear();
}