namespace MarkdigWpfViewer.Showcase;

using System.Globalization;
using System.IO;

/// <summary>Тексты образцов, вшитые в сборку, на текущем языке.</summary>
internal static class Samples
{
    /// <summary>Общий документ со всем, что вьюер умеет рисовать; его показывают несколько витрин.</summary>
    public static string Document => Read($"{typeof(Samples).Namespace}.Samples", "Document.md");

    /// <summary>Образец из папки типа <typeparamref name="T"/>.</summary>
    public static string Near<T>(string name) => Read(typeof(T).Namespace!, name);

    /// <summary>Образец на текущем языке: <c>Имя.ru.md</c> по-русски; перевода нет — английский <c>Имя.md</c>.</summary>
    private static string Read(string folder, string name)
    {
        var assembly = typeof(Samples).Assembly;
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var localized = $"{folder}.{Path.GetFileNameWithoutExtension(name)}.{language}{Path.GetExtension(name)}";
        using var stream = assembly.GetManifestResourceStream(localized)
                           ?? assembly.GetManifestResourceStream($"{folder}.{name}")
                           ?? throw new InvalidOperationException($"Нет образца {folder}.{name}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}