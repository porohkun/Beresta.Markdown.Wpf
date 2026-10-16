namespace MarkdigWpfViewer.Kit;

/// <summary>Шорткод <c>{%имя аргументы%}</c> или <c>{{&lt; имя аргументы &gt;}}</c>.</summary>
/// <param name="Name">Имя.</param>
/// <param name="Arguments">Аргументы по порядку; кавычки сняты.</param>
/// <param name="Named">Именованные аргументы <c>ключ=значение</c>.</param>
public sealed record MdShortcode(string Name, IReadOnlyList<string> Arguments, IReadOnlyDictionary<string, string> Named)
{
    /// <summary>Именованный аргумент, а нет его — аргумент по номеру; нет и его — <c>null</c>.</summary>
    public string? Argument(string name, int index)
        => Named.TryGetValue(name, out var value) ? value : index < Arguments.Count ? Arguments[index] : null;
}
