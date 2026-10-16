namespace MarkdigWpfViewer.Showcase.Showcases.Kit;

using MarkdigWpfViewer.Kit;

/// <summary>Что витрина знает о связях: какие вики-страницы есть и как зовут участников.</summary>
public sealed class KitResolver : IMdLinkResolver
{
    private static readonly HashSet<string> Pages = new(StringComparer.OrdinalIgnoreCase) { "Витрина", "Расширения" };

    private static readonly Dictionary<string, string> People = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ivan"] = "Иван Петров",
        ["maria"] = "Мария Сидорова"
    };

    public static KitResolver Instance { get; } = new();

    public bool PageExists(string page) => Pages.Contains(page);

    public string? DisplayName(string user) => People.GetValueOrDefault(user);
}
