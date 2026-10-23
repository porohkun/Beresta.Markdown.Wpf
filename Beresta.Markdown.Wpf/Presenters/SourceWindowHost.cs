namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Controls;
using Selection;
using ViewModels;

/// <summary>
/// Место окна наследника в контейнере блока: держит элемент окна, а когда контейнер пересоздан,
/// забирает элемент у прежнего места. Мышь внутри окна выделение вьюера не начинает.
/// </summary>
internal sealed class SourceWindowHost : Decorator
{
    public SourceWindowHost()
    {
        MdSelection.SetIsInteractive(this, true);
        DataContextChanged += (_, _) => Adopt();
    }

    /// <summary>Шаблон окна для выбора шаблона блока.</summary>
    public static DataTemplate Template { get; } = new() { VisualTree = new FrameworkElementFactory(typeof(SourceWindowHost)) };

    private void Adopt()
    {
        var element = (DataContext as SourceWindowVm)?.Element;
        if (ReferenceEquals(Child, element))
        {
            return;
        }

        if (element?.Parent is SourceWindowHost previous)
        {
            previous.Child = null;
        }

        Child = element;
    }
}
