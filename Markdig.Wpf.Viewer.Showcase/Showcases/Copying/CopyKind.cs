namespace MarkdigWpfViewer.Showcase.Showcases.Copying;

/// <summary>Что приложение кладёт в буфер обмена по Ctrl+C.</summary>
public enum CopyKind
{
    /// <summary>Как вьюер: голый текст и разметку разом.</summary>
    Viewer,

    /// <summary>Только голый текст.</summary>
    Text,

    /// <summary>Разметку обычным текстом: вставится в любую программу.</summary>
    Markdown
}