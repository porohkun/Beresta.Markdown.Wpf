namespace Beresta.Markdown.Wpf.Kit;

/// <summary>A shortcode <c>{%name arguments%}</c> or <c>{{&lt; name arguments &gt;}}</c>.</summary>
/// <param name="Name">The name.</param>
/// <param name="Arguments">The arguments in order; quotes removed.</param>
/// <param name="Named">Named arguments <c>key=value</c>.</param>
public sealed record MdShortcode(string Name, IReadOnlyList<string> Arguments, IReadOnlyDictionary<string, string> Named)
{
    /// <summary>A named argument, and without it the argument by number; without that too — <c>null</c>.</summary>
    public string? Argument(string name, int index)
        => Named.TryGetValue(name, out var value) ? value : index < Arguments.Count ? Arguments[index] : null;
}
