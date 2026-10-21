namespace Beresta.Markdown.Wpf.Extensions;

using System.ComponentModel;
using ViewModels;

/// <summary>
/// Состояния виджетов вьюера по устойчивым идентификаторам элементов. Сменившееся состояние
/// уходит вьюеру: он спрашивает, что записать в исходник.
/// </summary>
internal sealed class WidgetStates
{
    private readonly Func<IReadOnlyList<MdExtension>> _extensions;
    private readonly ExtensionGuard _guard;
    private readonly Func<bool> _followSource;
    private readonly Action<NodeVm, object?> _changed;
    private readonly Dictionary<long, (Type Kind, object State)> _states = [];
    private readonly Dictionary<long, NodeVm> _nodes = [];
    private bool _syncing;

    public WidgetStates(
        Func<IReadOnlyList<MdExtension>> extensions,
        ExtensionGuard guard,
        Func<bool> followSource,
        Action<NodeVm, object?> changed)
    {
        _extensions = extensions;
        _guard = guard;
        _followSource = followSource;
        _changed = changed;
    }

    /// <summary>Состояние виджета; первый раз его создаёт расширение. <c>null</c> — состояния нет.</summary>
    public object? StateOf(object? content)
    {
        if (content is not NodeVm { NodeId: not 0 } node || node is SourceWindowVm)
        {
            return null;
        }

        _nodes[node.NodeId] = node;
        if (_states.TryGetValue(node.NodeId, out var known) && known.Kind == node.GetType())
        {
            Follow(node, known.State);
            return known.State;
        }

        foreach (var extension in _extensions())
        {
            if (_guard.Call(extension, () => extension.CreateState(node), null) is { } state)
            {
                Remember(node, state);
                return state;
            }
        }

        if (FoldStateOf(node) is { } fold)
        {
            Remember(node, fold);
            return fold;
        }

        return null;
    }

    /// <summary>Забывает состояния элементов, которых в документе больше нет.</summary>
    public void Retain(Func<long, bool> exists)
    {
        foreach (var id in _states.Keys.Where(id => !exists(id)).ToList())
        {
            _states.Remove(id);
        }

        foreach (var id in _nodes.Keys.Where(id => !exists(id)).ToList())
        {
            _nodes.Remove(id);
        }
    }

    public void Clear()
    {
        _states.Clear();
        _nodes.Clear();
    }

    /// <summary>Свёрнутость встроенного сворачиваемого блока; у других блоков — <c>null</c>.</summary>
    private static MdFoldState? FoldStateOf(NodeVm node) => node switch
    {
        CalloutVm { IsFoldable: true } callout => new(!callout.IsFolded),
        DetailsVm details => new(details.IsOpen),
        _ => null
    };

    private void Remember(NodeVm node, object state)
    {
        _states[node.NodeId] = (node.GetType(), state);
        if (state is INotifyPropertyChanged notifying)
        {
            var id = node.NodeId;
            notifying.PropertyChanged += (_, _) => OnChanged(id, state);
        }
    }

    private void OnChanged(long id, object state)
    {
        if (!_syncing && _nodes.TryGetValue(id, out var node))
        {
            _changed(node, state);
        }
    }

    /// <summary>
    /// Пока контролы правят исходник, свёрнутость встроенного блока следует исходнику: его могли
    /// поменять мимо контрола — отменой, правкой текста.
    /// </summary>
    private void Follow(NodeVm node, object state)
    {
        if (!_followSource() || state is not MdFoldState fold || FoldStateOf(node) is not { } source || source.IsExpanded == fold.IsExpanded)
        {
            return;
        }

        _syncing = true;
        try
        {
            fold.IsExpanded = source.IsExpanded;
        }
        finally
        {
            _syncing = false;
        }
    }
}
