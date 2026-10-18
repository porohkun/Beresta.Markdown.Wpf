namespace Beresta.Markdown.Wpf.Showcase.Showcases.Sets;

using Localization;
using Beresta.Markdown.Wpf.Kit;

/// <summary>Что витрина знает о связях в образцах наборов: какой страницы нет и как зовут участников.</summary>
public sealed class SetsResolver : IMdLinkResolver
{
    /// <summary>Логины участников; имена — строки витрины <c>People.&lt;логин&gt;</c>.</summary>
    private static readonly HashSet<string> People = new(StringComparer.OrdinalIgnoreCase) { "anna", "boris" };

    public static SetsResolver Instance { get; } = new();

    public bool PageExists(string page) => page is not ("Missing page" or "Несуществующая страница");

    public string? DisplayName(string user) => People.TryGetValue(user, out var login) ? Localizer.Text($"People.{login}") : null;
}
