namespace MarkdigWpfViewer.Kit;

using Extensions;

/// <summary>Набор готовых расширений под одну программу: части в порядке библиотеки, без выключенных в <see cref="Without"/>.</summary>
public abstract class KitSet : MdExtensionSet
{
    private readonly List<(KitParts Part, MdExtension Extension)> _parts = [];
    private KitParts _without;

    /// <summary>Части набора, которые не подключаются.</summary>
    public KitParts Without
    {
        get => _without;
        set
        {
            _without = value;
            Fill();
        }
    }

    /// <summary>Добавляет часть в конец набора.</summary>
    protected T Part<T>(KitParts part, T extension)
        where T : MdExtension
    {
        _parts.Add((part, extension));
        Fill();
        return extension;
    }

    /// <summary>Включённые части — в начало расширений набора по порядку; добавленное приложением остаётся за ними.</summary>
    private void Fill()
    {
        foreach (var (_, extension) in _parts)
        {
            Extensions.Remove(extension);
        }

        var enabled = _parts
            .Where(entry => (_without & entry.Part) == 0)
            .Select(entry => entry.Extension)
            .ToList();
        for (var i = 0; i < enabled.Count; i++)
        {
            Extensions.Insert(i, enabled[i]);
        }
    }
}
