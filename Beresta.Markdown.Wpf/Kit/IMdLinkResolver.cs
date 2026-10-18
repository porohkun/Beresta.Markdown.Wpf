namespace Beresta.Markdown.Wpf.Kit;

/// <summary>
/// What the application knows about the targets of links of ready-made extensions: whether a page
/// exists, what a person is called.
/// </summary>
public interface IMdLinkResolver
{
    /// <summary>Whether a wiki page with this name exists; by default it does.</summary>
    bool PageExists(string page) => true;

    /// <summary>The display name of a person by the name from the markup; <c>null</c> — as written.</summary>
    string? DisplayName(string user) => null;
}
