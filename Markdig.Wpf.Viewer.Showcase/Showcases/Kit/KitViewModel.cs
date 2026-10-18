namespace MarkdigWpfViewer.Showcase.Showcases.Kit;

using Localization;

/// <summary>Готовые расширения библиотеки: синтаксис других программ, подключённый одной строкой.</summary>
public sealed class KitViewModel : ShowcaseViewModel
{
    public KitViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public string Text => Samples.Near<KitViewModel>("Kit.md");

    /// <summary>Цель последней нажатой ссылки своей схемы: <c>wiki:</c>, <c>tag:</c>, <c>mention:</c>; не нажимали — <c>null</c>.</summary>
    public string? LastHref
    {
        get;
        set => Set(ref field, value, () => RaisePropertyChanged(nameof(LastLink)));
    }

    /// <summary>Подсказка нажать ссылку либо её цель — на текущем языке.</summary>
    public string LastLink => LastHref is { } href
        ? string.Format(Localizer.Text("Links.Clicked"), href)
        : Localizer.Text("Links.Prompt");
}
