namespace Beresta.Markdown.Wpf.Kit;

/// <summary>Новое значение свойства из поля карточки — то, что карточка просит записать в исходник.</summary>
internal sealed record PropertyEdit(PropertyVm Property, string Text);
