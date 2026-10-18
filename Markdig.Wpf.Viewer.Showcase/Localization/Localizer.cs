namespace MarkdigWpfViewer.Showcase.Localization;

using System.Globalization;
using System.Resources;
using System.Windows;
using HandyControl.Tools;

/// <summary>Язык витрины: строки из <c>Strings.resx</c> и переключение вместе с HandyControl и подписями вьюера.</summary>
public static class Localizer
{
    private static readonly ResourceManager Strings = new($"{typeof(Localizer).Namespace}.Strings", typeof(Localizer).Assembly);

    private static readonly Uri RussianCaptions =
        new("pack://application:,,,/Markdig.Wpf.Viewer.Showcase;component/Localization/RussianCaptions.xaml");

    /// <summary>Язык сменился: строки, подписи и контролы уже на новом.</summary>
    public static event Action? Changed;

    /// <summary>Язык системы при запуске — русский.</summary>
    public static bool IsSystemRussian { get; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";

    /// <summary>Строка по ключу на текущем языке; ключа нет — ключ в скобках.</summary>
    public static string Text(string key) => Strings.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";

    /// <summary>Переключает язык строк витрины, контролов HandyControl и подписей вьюера.</summary>
    public static void Apply(bool russian)
    {
        var language = russian ? "ru" : "en";
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(language);
        ConfigHelper.Instance.SetLang(language);

        if (Application.Current is { } application)
        {
            var merged = application.Resources.MergedDictionaries;
            var captions = merged.FirstOrDefault(dictionary => dictionary.Source == RussianCaptions);
            if (russian && captions is null)
            {
                merged.Add(new() { Source = RussianCaptions });
            }
            else if (!russian && captions is not null)
            {
                merged.Remove(captions);
            }
        }

        Changed?.Invoke();
    }
}
