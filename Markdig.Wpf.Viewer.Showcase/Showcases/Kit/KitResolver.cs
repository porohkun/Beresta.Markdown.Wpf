namespace MarkdigWpfViewer.Showcase.Showcases.Kit;

using Localization;
using MarkdigWpfViewer.Kit;

/// <summary>Что витрина знает о связях: какие вики-страницы есть и как зовут участников.</summary>
public sealed class KitResolver : IMdLinkResolver
{
    private static readonly HashSet<string> Pages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Showcase",
        "Extensions",
        "Витрина",
        "Расширения"
    };

    /// <summary>Логины участников; имена — строки витрины <c>People.&lt;логин&gt;</c>.</summary>
    private static readonly HashSet<string> People = new(StringComparer.OrdinalIgnoreCase) { "ivan", "maria" };

    public static KitResolver Instance { get; } = new();

    public bool PageExists(string page) => Pages.Contains(page);

    public string? DisplayName(string user) => People.TryGetValue(user, out var login) ? Localizer.Text($"People.{login}") : null;
}
