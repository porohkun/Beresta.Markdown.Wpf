namespace Beresta.Markdown.Wpf.Kit;

using Selection;
using ViewModels;

/// <summary>Код в строке, который целиком — цвет: код и кружок этого цвета.</summary>
/// <param name="Code">Код как написан.</param>
/// <param name="Color">Цвет в записи WPF <c>#AARRGGBB</c>.</param>
public sealed record ColorCodeVm(string Code, string Color) : InlineVm, IMdCopyText
{
    public string CopyText => Code;
}
