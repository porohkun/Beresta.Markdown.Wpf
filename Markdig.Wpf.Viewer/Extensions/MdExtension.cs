namespace MarkdigWpfViewer.Extensions;

using System.Windows;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Расширение вьюера под свой синтаксис: настройка разбора, превращение своих узлов Markdig в
/// виджеты и шаблоны для них. Подключается через <see cref="MarkdownViewer.Extensions"/>.
/// </summary>
public abstract class MdExtension
{
    /// <summary>Добавляет в разбор свои расширения Markdig.</summary>
    public virtual void SetupPipeline(MarkdownPipelineBuilder pipeline)
    {
    }

    /// <summary>Превращает узел в блочный виджет; <c>null</c> — узел не свой.</summary>
    public virtual BlockVm? ConvertBlock(Block block, MdConvertContext context) => null;

    /// <summary>Превращает инлайн в строчный виджет; <c>null</c> — инлайн не свой.</summary>
    public virtual InlineVm? ConvertInline(Inline inline, MdConvertContext context) => null;

    /// <summary>
    /// Превращает инлайн в несколько инлайнов модели: текст, оформленные куски, виджеты; пустой
    /// список — инлайн не рисуется, <c>null</c> — инлайн не свой. По умолчанию — <see cref="ConvertInline"/>.
    /// </summary>
    public virtual IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => ConvertInline(inline, context) is { } single ? [single] : null;

    /// <summary>Шаблон своего виджета; <c>null</c> — элемент не свой.</summary>
    public virtual DataTemplate? SelectTemplate(object item, DependencyObject container) => null;

    /// <summary>
    /// Состояние своего виджета, которое должно пережить его контрол: свёрнут ли, что выбрано.
    /// Шаблон получает его через <see cref="MdWidget.StateProperty"/>; <c>null</c> — состояния нет.
    /// </summary>
    public virtual object? CreateState(NodeVm node) => null;
}