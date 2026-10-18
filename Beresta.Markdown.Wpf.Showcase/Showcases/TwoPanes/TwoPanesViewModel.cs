namespace Beresta.Markdown.Wpf.Showcase.Showcases.TwoPanes;

/// <summary>Две версии статьи рядом, как в диффе: синхронная прокрутка с соединениями или панели вровень.</summary>
public sealed class TwoPanesViewModel : ShowcaseViewModel
{
    public TwoPanesViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public string Left => Samples.Near<TwoPanesViewModel>("Left.md");

    public string Right => Samples.Near<TwoPanesViewModel>("Right.md");

    /// <summary>Панели вровень с пустыми местами; иначе — синхронная прокрутка.</summary>
    public bool IsAligned
    {
        get;
        set => Set(ref field, value);
    }
}
