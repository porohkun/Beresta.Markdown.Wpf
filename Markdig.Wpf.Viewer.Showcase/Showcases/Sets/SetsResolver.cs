namespace MarkdigWpfViewer.Showcase.Showcases.Sets;

using MarkdigWpfViewer.Kit;

/// <summary>Что витрина знает о связях в образцах наборов: какой страницы нет и как зовут участников.</summary>
public sealed class SetsResolver : IMdLinkResolver
{
    private static readonly Dictionary<string, string> People = new(StringComparer.OrdinalIgnoreCase)
    {
        ["anna"] = "Анна Смирнова",
        ["boris"] = "Борис Иванов"
    };

    public static SetsResolver Instance { get; } = new();

    public bool PageExists(string page) => page != "Несуществующая страница";

    public string? DisplayName(string user) => People.GetValueOrDefault(user);
}
