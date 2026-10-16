namespace MarkdigWpfViewer.ViewModels;

/// <summary>Выравнивание столбца таблицы из строки-разделителя: <c>:---</c>, <c>:---:</c>, <c>---:</c>.</summary>
public enum TableAlignment
{
    /// <summary>Не задано — как у текста вокруг.</summary>
    Default,

    Left,

    Center,

    Right
}