namespace MarkdigWpfViewer.ViewModels;

/// <summary>Обрамление куска текста в разметке выделенного: что пишется до текста и что после.</summary>
public sealed record MarkupFrame(string Open, string Close);
