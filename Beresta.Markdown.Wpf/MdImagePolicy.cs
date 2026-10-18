namespace Beresta.Markdown.Wpf;

/// <summary>
/// Which images the viewer passes to the loader at all: address schemes, hosts of the web, files on
/// other computers.
/// </summary>
public sealed class MdImagePolicy
{
    /// <summary>Everything the loader can, except files on other computers.</summary>
    public static MdImagePolicy Default { get; } = new();

    /// <summary>No network: own files, application resources and images inside the document.</summary>
    public static MdImagePolicy Offline { get; } = new() { Schemes = ["file", "pack", "data"] };

    /// <summary>
    /// For text from others — a chat, the answer of a model: only application resources and images
    /// inside the document.
    /// </summary>
    public static MdImagePolicy Untrusted { get; } = new() { Schemes = ["pack", "data"] };

    /// <summary>Allowed address schemes; <c>null</c> — any.</summary>
    public IReadOnlyCollection<string>? Schemes { get; init; }

    /// <summary>Allowed hosts of <c>http</c> and <c>https</c> addresses; <c>null</c> — any.</summary>
    public IReadOnlyCollection<string>? Hosts { get; init; }

    /// <summary>Whether files on other computers are allowed: <c>file://server/…</c> and UNC paths.</summary>
    public bool AllowNetworkFiles { get; init; }

    /// <summary>
    /// Whether to pass an image to the loader: the address from the markup and the same address
    /// resolved against the address of the document.
    /// </summary>
    public bool Allows(string url, Uri? uri)
    {
        var scheme = url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ? "data"
            : uri is { IsAbsoluteUri: true } ? uri.Scheme
            : null;

        if (Schemes is not null && (scheme is null || !Schemes.Contains(scheme, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (scheme is "http" or "https" && Hosts is not null && !Hosts.Contains(uri!.Host, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return scheme != "file" || AllowNetworkFiles || !uri!.IsUnc;
    }
}
