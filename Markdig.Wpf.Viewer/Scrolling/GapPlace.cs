namespace MarkdigWpfViewer.Scrolling;

/// <summary>Как пустое место стоит относительно своего элемента.</summary>
internal enum GapPlace
{
    /// <summary>Перед элементом в его списке.</summary>
    Before,

    /// <summary>После элемента в его списке.</summary>
    After,

    /// <summary>Внутри текста или кода элемента, перед экранной строкой.</summary>
    Row
}
