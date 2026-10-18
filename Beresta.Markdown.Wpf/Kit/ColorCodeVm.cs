namespace Beresta.Markdown.Wpf.Kit;

using Selection;
using ViewModels;

/// <summary>Inline code that is a color as a whole: the code and a dot of that color.</summary>
/// <param name="Code">The code as written.</param>
/// <param name="Color">The color in WPF notation <c>#AARRGGBB</c>.</param>
public sealed record ColorCodeVm(string Code, string Color) : InlineVm, IMdCopyText
{
    /// <inheritdoc/>
    public string CopyText => Code;
}
