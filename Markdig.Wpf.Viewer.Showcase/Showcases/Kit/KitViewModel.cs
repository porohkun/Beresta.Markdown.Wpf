namespace MarkdigWpfViewer.Showcase.Showcases.Kit;

/// <summary>Готовые расширения библиотеки: синтаксис других программ, подключённый одной строкой.</summary>
public sealed class KitViewModel : ShowcaseViewModel
{
    public KitViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public string Text { get; } = Samples.Near<KitViewModel>("Kit.md");

    /// <summary>Последняя нажатая ссылка своей схемы: <c>wiki:</c>, <c>tag:</c>, <c>mention:</c>.</summary>
    public string LastLink
    {
        get;
        set => Set(ref field, value);
    } = "Нажми вики-ссылку, тег или упоминание — здесь появится их цель.";
}
