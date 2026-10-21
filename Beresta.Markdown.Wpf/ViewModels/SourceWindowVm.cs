namespace Beresta.Markdown.Wpf.ViewModels;

using System.Windows;
using Scrolling;

/// <summary>Окно наследника в списке блоков: его элемент стоит вместо блоков на строках окна.</summary>
internal sealed record SourceWindowVm(FrameworkElement Element, IMdSourceRows Rows) : BlockVm
{
    /// <summary>Идентификатор окна — один на все разборы; разбор таких не выдаёт.</summary>
    public const long Id = -1;
}
