namespace Beresta.Markdown.Wpf.Kit;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>Цвет CSS — <c>#RGB</c>, <c>#RRGGBBAA</c>, <c>rgb()</c>, <c>hsl()</c> — в записи WPF <c>#AARRGGBB</c>.</summary>
internal static partial class CssColor
{
    /// <summary>Разбирает цвет целиком; не цвет — <c>false</c>.</summary>
    public static bool TryParse(string text, out string argb)
    {
        argb = string.Empty;
        var value = text.Trim();

        if (Hex().Match(value) is { Success: true } hex)
        {
            var digits = hex.Groups["digits"].Value;
            if (digits.Length <= 4)
            {
                digits = string.Concat(digits.Select(digit => new string(digit, 2)));
            }

            var alpha = digits.Length == 8 ? digits[6..] : "FF";
            argb = $"#{alpha}{digits[..6]}".ToUpperInvariant();
            return true;
        }

        if (Function().Match(value) is not { Success: true } function)
        {
            return false;
        }

        var parts = function.Groups["arguments"].Value.Split([',', ' ', '/'], StringSplitOptions.RemoveEmptyEntries);
        var name = function.Groups["name"].Value.ToLowerInvariant();
        if (parts.Length is not (3 or 4))
        {
            return false;
        }

        if (!TryAlpha(parts.Length == 4 ? parts[3] : "1", out var a))
        {
            return false;
        }

        byte r, g, b;
        if (name.StartsWith("rgb", StringComparison.Ordinal))
        {
            if (!TryChannel(parts[0], out r) || !TryChannel(parts[1], out g) || !TryChannel(parts[2], out b))
            {
                return false;
            }
        }
        else if (!TryHsl(parts[0], parts[1], parts[2], out r, out g, out b))
        {
            return false;
        }

        argb = $"#{a:X2}{r:X2}{g:X2}{b:X2}";
        return true;
    }

    private static bool TryChannel(string part, out byte channel)
    {
        channel = 0;
        if (part.EndsWith('%'))
        {
            if (!double.TryParse(part[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) || percent is < 0 or > 100)
            {
                return false;
            }

            channel = (byte)Math.Round(percent * 2.55);
            return true;
        }

        if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || number is < 0 or > 255)
        {
            return false;
        }

        channel = (byte)Math.Round(number);
        return true;
    }

    private static bool TryAlpha(string part, out byte alpha)
    {
        alpha = 0;
        var percent = part.EndsWith('%');
        if (!double.TryParse(percent ? part[..^1] : part, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        var fraction = percent ? number / 100 : number;
        if (fraction is < 0 or > 1)
        {
            return false;
        }

        alpha = (byte)Math.Round(fraction * 255);
        return true;
    }

    private static bool TryHsl(string hue, string saturation, string lightness, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        if (!double.TryParse(hue.TrimEnd('d', 'e', 'g'), NumberStyles.Float, CultureInfo.InvariantCulture, out var h)
            || !saturation.EndsWith('%') || !double.TryParse(saturation[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var s)
            || !lightness.EndsWith('%') || !double.TryParse(lightness[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var l)
            || s is < 0 or > 100 || l is < 0 or > 100)
        {
            return false;
        }

        h = ((h % 360) + 360) % 360 / 360;
        s /= 100;
        l /= 100;
        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        r = Channel(p, q, h + 1.0 / 3);
        g = Channel(p, q, h);
        b = Channel(p, q, h - 1.0 / 3);
        return true;

        static byte Channel(double p, double q, double t)
        {
            t = t < 0 ? t + 1 : t > 1 ? t - 1 : t;
            var value = t < 1.0 / 6 ? p + (q - p) * 6 * t
                : t < 0.5 ? q
                : t < 2.0 / 3 ? p + (q - p) * (2.0 / 3 - t) * 6
                : p;
            return (byte)Math.Round(value * 255);
        }
    }

    [GeneratedRegex("^#(?<digits>[0-9A-Fa-f]{3}|[0-9A-Fa-f]{4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")]
    private static partial Regex Hex();

    [GeneratedRegex(@"^(?<name>rgba?|hsla?)\(\s*(?<arguments>[^()]*?)\s*\)$", RegexOptions.IgnoreCase)]
    private static partial Regex Function();
}
