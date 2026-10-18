namespace Beresta.Markdown.Wpf.Showcase.Showcases.CustomLook;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Abstractions;

/// <summary>Документ со своим шрифтом и своим провайдером шаблонов; свои значения ключей темы — в ресурсах вьюера.</summary>
public partial class CustomLookView : UserControl
{
    private static readonly FontFamily CustomFont = new("Georgia");

    public CustomLookView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is CustomLookViewModel old)
        {
            PropertyChangedEventManager.RemoveHandler(old, OnModelChanged, string.Empty);
            Viewer.Resources.MergedDictionaries.Remove(old.Keys.Overrides);
        }

        if (e.NewValue is CustomLookViewModel model)
        {
            PropertyChangedEventManager.AddHandler(model, OnModelChanged, string.Empty);
            Viewer.Resources.MergedDictionaries.Add(model.Keys.Overrides);
            ApplyLook(model);
        }
    }

    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is CustomLookViewModel model)
        {
            ApplyLook(model);
        }
    }

    /// <summary>Свой шрифт и свой шаблон заголовка — или стандартные.</summary>
    private void ApplyLook(CustomLookViewModel model)
    {
        if (model.IsCustomFont)
        {
            Viewer.FontFamily = CustomFont;
        }
        else
        {
            Viewer.ClearValue(FontFamilyProperty);
        }

        if (model.IsCustomHeadings)
        {
            Viewer.StyleProvider = (IMdStyleProvider)FindResource("AccentProvider");
        }
        else
        {
            Viewer.ClearValue(MarkdownViewer.StyleProviderProperty);
        }
    }

    private void OnReset(object sender, RoutedEventArgs e) => (DataContext as CustomLookViewModel)?.Keys.Reset();
}
