namespace MarkdigWpfViewer.Showcase.Showcases.TwoPanes;

/// <summary>Две версии статьи рядом, как в диффе: синхронная прокрутка и подсветка правок.</summary>
public sealed class TwoPanesViewModel : ShowcaseViewModel
{
    public TwoPanesViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Две панели";

    public string Left { get; } = Samples.Near<TwoPanesViewModel>("Left.md");

    public string Right { get; } = Samples.Near<TwoPanesViewModel>("Right.md");
}
