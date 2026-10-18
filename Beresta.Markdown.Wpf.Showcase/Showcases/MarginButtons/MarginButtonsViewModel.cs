namespace Beresta.Markdown.Wpf.Showcase.Showcases.MarginButtons;

/// <summary>Закладки строк кнопками в колонке приложения рядом с вьюером.</summary>
public sealed class MarginButtonsViewModel : ShowcaseViewModel
{
    public MarginButtonsViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public string Markdown => Samples.Near<MarginButtonsViewModel>("MarginButtons.md");
}
