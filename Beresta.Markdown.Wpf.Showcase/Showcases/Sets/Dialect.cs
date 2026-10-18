namespace Beresta.Markdown.Wpf.Showcase.Showcases.Sets;

using Beresta.Markdown.Wpf.Extensions;

/// <summary>Диалект: имя, по которому есть его название и документ в его синтаксисе, и набор, который этот синтаксис рисует.</summary>
public sealed record Dialect(string Name, MdExtension Set)
{
    /// <summary>Ключ названия в строках витрины.</summary>
    public string TitleKey => $"Sets.{Name}";

    /// <summary>Документ в синтаксисе диалекта.</summary>
    public string Text => Samples.Near<SetsViewModel>($"{Name}.md");
}
