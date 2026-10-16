namespace MarkdigWpfViewer.Kit;

using System.Windows;
using ViewModels;

/// <summary>Правку CriticMarkup приняли или отклонили: какое место исходника чем заменить; текст правит приложение.</summary>
public sealed class MdCriticDecidedEventArgs : RoutedEventArgs
{
    public MdCriticDecidedEventArgs(RoutedEvent routedEvent, object source, MdSourceSpan change, string replacement, bool isAccepted)
        : base(routedEvent, source)
    {
        Change = change;
        Replacement = replacement;
        IsAccepted = isAccepted;
    }

    /// <summary>Место правки в исходнике вместе с её разметкой.</summary>
    public MdSourceSpan Change { get; }

    /// <summary>Текст, который встаёт на место правки.</summary>
    public string Replacement { get; }

    /// <summary>Правку приняли; <c>false</c> — отклонили.</summary>
    public bool IsAccepted { get; }

    /// <inheritdoc />
    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget) =>
        ((EventHandler<MdCriticDecidedEventArgs>)genericHandler)(genericTarget, this);
}
