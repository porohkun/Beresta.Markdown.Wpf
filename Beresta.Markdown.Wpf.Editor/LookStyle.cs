namespace Beresta.Markdown.Wpf.Editor;

using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>
/// Шрифт и цвета вида исходника с именем <c>X</c> — из ключей темы <c>Md.X.*</c>; у жирного, курсива,
/// зачёркнутого и ссылки — свои. У кусков — шрифт, цвет и фон, у строк — фон и полоса слева.
/// </summary>
internal sealed record LookStyle
{
    public Brush? Foreground { get; init; }

    public Brush? Background { get; init; }

    public FontFamily? FontFamily { get; init; }

    public FontWeight? FontWeight { get; init; }

    public FontStyle? FontStyle { get; init; }

    public double? FontSize { get; init; }

    public bool Strike { get; init; }

    public Brush? Bar { get; init; }

    public double BarWidth { get; init; }

    /// <param name="look">Имя вида.</param>
    /// <param name="find">Ресурс по ключу; нет — <c>null</c>.</param>
    public static LookStyle Resolve(string look, Func<string, object?> find) => look switch
    {
        "Bold" => new() { FontWeight = FontWeights.Bold },
        "Italic" => new() { FontStyle = FontStyles.Italic },
        "Strike" => new() { Strike = true },
        "Link" => new() { Foreground = find("Md.Paragraph.LinkForeground") as Brush },
        _ => new()
        {
            Foreground = find($"Md.{look}.Foreground") as Brush,
            Background = find($"Md.{look}.Background") as Brush,
            FontFamily = find($"Md.{look}.FontFamily") as FontFamily,
            FontWeight = find($"Md.{look}.FontWeight") as FontWeight?,
            FontStyle = find($"Md.{look}.FontStyle") as FontStyle?,
            FontSize = find($"Md.{look}.FontSize") as double?,
            Bar = find($"Md.{look}.BorderBrush") as Brush,
            BarWidth = find($"Md.{look}.BorderThickness") is Thickness thickness ? thickness.Left : 0
        }
    };

    /// <summary>Шрифт, цвет и фон куска.</summary>
    public void Apply(VisualLineElement element)
    {
        var properties = element.TextRunProperties;
        if (Foreground is not null)
        {
            properties.SetForegroundBrush(Foreground);
        }

        if (Background is not null)
        {
            properties.SetBackgroundBrush(Background);
        }

        if (FontSize is { } size)
        {
            properties.SetFontRenderingEmSize(size);
        }

        if (Strike)
        {
            properties.SetTextDecorations(TextDecorations.Strikethrough);
        }

        if (FontFamily is not null || FontWeight is not null || FontStyle is not null)
        {
            var typeface = properties.Typeface;
            properties.SetTypeface(new(FontFamily ?? typeface.FontFamily, FontStyle ?? typeface.Style, FontWeight ?? typeface.Weight, typeface.Stretch));
        }
    }
}
