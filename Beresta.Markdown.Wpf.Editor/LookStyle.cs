namespace Beresta.Markdown.Wpf.Editor;

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>
/// Шрифт и цвета вида исходника с именем <c>X</c> — из ключей темы <c>Md.X.*</c>, а без своего ключа — из
/// сеттеров стиля <c>Md.X.Style</c>; у жирного, курсива, зачёркнутого, ссылки и плашки — свои. У кусков —
/// шрифт, цвет и фон, у строк — фон и полоса слева.
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

    public bool Underline { get; init; }

    public Brush? Bar { get; init; }

    public double BarWidth { get; init; }

    /// <summary>Поля блока в отрисовке: исходник поднимает строки до той же высоты.</summary>
    public Thickness? Margin { get; init; }

    /// <param name="look">Имя вида.</param>
    /// <param name="find">Ресурс по ключу; нет — <c>null</c>.</param>
    public static LookStyle Resolve(string look, Func<string, object?> find) => look switch
    {
        "Bold" => new() { FontWeight = FontWeights.Bold },
        "Italic" => new() { FontStyle = FontStyles.Italic },
        "Strike" => new() { Strike = true },
        "Link" => new() { Foreground = find("Md.Paragraph.LinkForeground") as Brush },
        _ when look.StartsWith("Callout.", StringComparison.Ordinal) => Callout(look["Callout.".Length..], find),
        _ => Combined(Keyed(look, find), Styled(find($"Md.{look}.Style") as Style, find))
    };

    /// <summary>Свои ключи вида поверх сеттеров его стиля: у свойства без своего ключа — значение стиля.</summary>
    private static LookStyle Combined(LookStyle keyed, LookStyle? styled) => styled is null ? keyed : keyed with
    {
        Foreground = keyed.Foreground ?? styled.Foreground,
        Background = keyed.Background ?? styled.Background,
        FontFamily = keyed.FontFamily ?? styled.FontFamily,
        FontWeight = keyed.FontWeight ?? styled.FontWeight,
        FontStyle = keyed.FontStyle ?? styled.FontStyle,
        FontSize = keyed.FontSize ?? styled.FontSize,
        Strike = styled.Strike,
        Underline = styled.Underline
    };

    /// <summary>
    /// Вид плашки: полоса её строк и вид в заголовке — кистью вида <c>Md.Callout.X.Brush</c>, у вида без
    /// своей кисти — как у <c>Note</c>; поля — поля плашки.
    /// </summary>
    private static LookStyle Callout(string kind, Func<string, object?> find)
    {
        var brush = (find($"Md.Callout.{kind}.Brush") ?? find("Md.Callout.Note.Brush")) as Brush;
        return new()
        {
            Foreground = brush,
            Bar = brush,
            BarWidth = find("Md.Callout.BorderThickness") is Thickness thickness ? thickness.Left : 0,
            Margin = find("Md.Callout.Margin") as Thickness?
        };
    }

    /// <summary>Вид по своим ключам темы <c>Md.X.*</c>.</summary>
    private static LookStyle Keyed(string look, Func<string, object?> find)
        => new()
        {
            Foreground = find($"Md.{look}.Foreground") as Brush,
            Background = find($"Md.{look}.Background") as Brush,
            FontFamily = find($"Md.{look}.FontFamily") as FontFamily,
            FontWeight = find($"Md.{look}.FontWeight") as FontWeight?,
            FontStyle = find($"Md.{look}.FontStyle") as FontStyle?,
            FontSize = find($"Md.{look}.FontSize") as double?,
            Bar = find($"Md.{look}.BorderBrush") as Brush,
            BarWidth = find($"Md.{look}.BorderThickness") is Thickness thickness ? thickness.Left : 0,
            Margin = find($"Md.{look}.Margin") as Thickness?
        };

    /// <summary>
    /// Вид по сеттерам стиля <c>Md.X.Style</c> — того же, которым рисуется оформленный текст: цвет, фон,
    /// шрифт, черта; сеттеры основы стиля — раньше его собственных. Стиля нет — <c>null</c>.
    /// </summary>
    private static LookStyle? Styled(Style? style, Func<string, object?> find)
    {
        if (style is null)
        {
            return null;
        }

        var result = Styled(style.BasedOn, find) ?? new LookStyle();
        foreach (var setter in style.Setters.OfType<Setter>())
        {
            var value = setter.Value is DynamicResourceExtension { ResourceKey: { } key } ? find(key.ToString()!) : setter.Value;
            result = setter.Property.Name switch
            {
                nameof(TextElement.Foreground) => result with { Foreground = value as Brush },
                nameof(TextElement.Background) => result with { Background = value as Brush },
                nameof(TextElement.FontFamily) => result with { FontFamily = value as FontFamily },
                nameof(TextElement.FontWeight) => result with { FontWeight = value as FontWeight? },
                nameof(TextElement.FontStyle) => result with { FontStyle = value as FontStyle? },
                nameof(TextElement.FontSize) => result with { FontSize = value as double? },
                nameof(Inline.TextDecorations) => result with
                {
                    Strike = value is TextDecorationCollection strike && strike.Any(decoration => decoration.Location == TextDecorationLocation.Strikethrough),
                    Underline = value is TextDecorationCollection underline && underline.Any(decoration => decoration.Location == TextDecorationLocation.Underline)
                },
                _ => result
            };
        }

        return result;
    }

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

        if (Strike || Underline)
        {
            var decorations = new TextDecorationCollection();
            if (Strike)
            {
                decorations.Add(TextDecorations.Strikethrough);
            }

            if (Underline)
            {
                decorations.Add(TextDecorations.Underline);
            }

            properties.SetTextDecorations(decorations);
        }

        if (FontFamily is not null || FontWeight is not null || FontStyle is not null)
        {
            var typeface = properties.Typeface;
            properties.SetTypeface(new(FontFamily ?? typeface.FontFamily, FontStyle ?? typeface.Style, FontWeight ?? typeface.Weight, typeface.Stretch));
        }
    }
}
