namespace MarkdigWpfViewer.Extensions;

/// <summary>Состояние оформленного куска текста, которое меняет щелчок по куску.</summary>
public interface IMdClickableState
{
    /// <summary>По куску щёлкнули, не выделяя текста.</summary>
    void OnClick();
}
