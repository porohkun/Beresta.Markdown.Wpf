namespace MarkdigWpfViewer.Showcase.Showcases.Kit;

/// <summary>Готовые расширения библиотеки: синтаксис других программ, подключённый одной строкой.</summary>
public sealed class KitViewModel : ShowcaseViewModel
{
    public KitViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Готовые расширения";

    public string Text { get; } = Samples.Near<KitViewModel>("Kit.md");
}
