namespace MarkdigWpfViewer.Kit;

using ViewModels;

/// <summary>Набор HackMD: контейнеры <c>:::вид</c>, оглавление, шорткоды <c>{%youtube id %}</c>.</summary>
public sealed class HackMDSet : KitSet
{
    private readonly Shortcodes _shortcodes;

    public HackMDSet()
    {
        Part(KitParts.CalloutContainers, new CalloutContainers());
        Part(KitParts.TableOfContents, new TableOfContents());
        _shortcodes = Part(KitParts.Shortcodes, new Shortcodes());
    }

    /// <summary>Обработчики шорткодов по имени; свои имена приложение добавляет сюда.</summary>
    public Dictionary<string, Func<MdShortcode, NodeVm?>> ShortcodeHandlers => _shortcodes.Handlers;

    /// <summary>Грузить превью видео из сети; выключено — карточка без превью.</summary>
    public bool LoadThumbnails
    {
        get => _shortcodes.LoadThumbnails;
        set => _shortcodes.LoadThumbnails = value;
    }
}
