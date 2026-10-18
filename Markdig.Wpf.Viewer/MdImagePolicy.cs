namespace MarkdigWpfViewer;

/// <summary>Какие картинки вьюер вообще отдаёт загрузчику: схемы адресов, хосты веба, файлы на других компьютерах.</summary>
public sealed class MdImagePolicy
{
    /// <summary>Всё, что умеет загрузчик, кроме файлов на других компьютерах.</summary>
    public static MdImagePolicy Default { get; } = new();

    /// <summary>Без сети: свои файлы, ресурсы приложения и картинки внутри документа.</summary>
    public static MdImagePolicy Offline { get; } = new() { Schemes = ["file", "pack", "data"] };

    /// <summary>Для чужого текста — чата, ответа модели: только ресурсы приложения и картинки внутри документа.</summary>
    public static MdImagePolicy Untrusted { get; } = new() { Schemes = ["pack", "data"] };

    /// <summary>Разрешённые схемы адресов; <c>null</c> — любые.</summary>
    public IReadOnlyCollection<string>? Schemes { get; init; }

    /// <summary>Разрешённые хосты адресов <c>http</c> и <c>https</c>; <c>null</c> — любые.</summary>
    public IReadOnlyCollection<string>? Hosts { get; init; }

    /// <summary>Можно ли файлы на других компьютерах: <c>file://сервер/…</c> и пути UNC.</summary>
    public bool AllowNetworkFiles { get; init; }

    /// <summary>Отдать ли загрузчику картинку: адрес из разметки и он же, разобранный от адреса документа.</summary>
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
