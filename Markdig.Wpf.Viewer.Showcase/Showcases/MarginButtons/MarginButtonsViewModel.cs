namespace MarkdigWpfViewer.Showcase.Showcases.MarginButtons;

/// <summary>Закладки строк кнопками в колонке приложения рядом с вьюером.</summary>
public sealed class MarginButtonsViewModel : ShowcaseViewModel
{
    public MarginButtonsViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Кнопки на полях";

    public string Markdown { get; } = Samples.Near<MarginButtonsViewModel>("MarginButtons.md");
}
