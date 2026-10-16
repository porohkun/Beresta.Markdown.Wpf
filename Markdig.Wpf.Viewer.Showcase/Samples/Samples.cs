namespace MarkdigWpfViewer.Showcase;

using System.IO;

/// <summary>Тексты образцов, вшитые в сборку.</summary>
internal static class Samples
{
    /// <summary>Общий документ со всем, что вьюер умеет рисовать; его показывают несколько витрин.</summary>
    public static string Document { get; } = Read($"{typeof(Samples).Namespace}.Samples.Document.md");

    /// <summary>Образец из папки типа <typeparamref name="T"/>.</summary>
    public static string Near<T>(string name) => Read($"{typeof(T).Namespace}.{name}");

    private static string Read(string resource)
    {
        using var stream = typeof(Samples).Assembly.GetManifestResourceStream(resource)
                           ?? throw new InvalidOperationException($"Нет образца {resource}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}