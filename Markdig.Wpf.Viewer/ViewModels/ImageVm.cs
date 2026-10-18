namespace MarkdigWpfViewer.ViewModels;

using Selection;

/// <summary>Картинка; <paramref name="Url"/> — адрес ровно из разметки, <paramref name="Alt"/> — её текст.</summary>
public sealed record ImageVm(string Url, string Alt) : InlineVm, IMdCopyText
{
    /// <summary>Заголовок картинки — подсказка; нет — <c>null</c>.</summary>
    public string? Title { get; init; }

    /// <summary>Ширина из атрибутов, в пикселях; не задана — <c>null</c>.</summary>
    public double? Width { get; init; }

    /// <summary>Высота из атрибутов, в пикселях; не задана — <c>null</c>.</summary>
    public double? Height { get; init; }

    /// <summary>Цель ссылки вокруг картинки; ссылки нет — <c>null</c>.</summary>
    public string? Href { get; init; }

    /// <summary>Разбор <see cref="Href"/>.</summary>
    public Uri? Hyperlink { get; init; }

    /// <summary>Заголовок ссылки вокруг картинки; нет — <c>null</c>.</summary>
    public string? LinkTitle { get; init; }

    public string CopyText => Alt;
}