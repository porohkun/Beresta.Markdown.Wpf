namespace Beresta.Markdown.Wpf.Showcase.Theming;

using System.Windows;
using HandyControl.Themes;

/// <summary>Переключает светлую и тёмную тему HandyControl вместе с ключами вьюера.</summary>
internal static class ThemeSwitcher
{
    public static void Apply(bool dark)
    {
        ThemeManager.Current.ApplicationTheme = dark ? ApplicationTheme.Dark : ApplicationTheme.Light;

        if (Application.Current is not { } application)
        {
            return;
        }

        // BYDESIGN: словари пересоздаются — их кисти берут цвета HandyControl через
        // DynamicResource и сами за сменой темы не следуют.
        var merged = application.Resources.MergedDictionaries;
        for (var i = 0; i < merged.Count; i++)
        {
            if (merged[i] is ThemedResourceDictionary { Source: { } source })
            {
                merged[i] = new ThemedResourceDictionary { Source = source };
            }
        }
    }
}