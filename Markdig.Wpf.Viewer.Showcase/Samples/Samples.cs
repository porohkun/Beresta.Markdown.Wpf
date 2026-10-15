namespace MarkdigWpfViewer.Showcase;

using System.IO;

/// <summary>Тексты образцов, вшитые в сборку.</summary>
internal static class Samples
{
    public static string Load(string name)
    {
        var resource = $"{typeof(Samples).Namespace}.Samples.{name}";
        using var stream = typeof(Samples).Assembly.GetManifestResourceStream(resource)
                           ?? throw new InvalidOperationException($"Нет образца {resource}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}