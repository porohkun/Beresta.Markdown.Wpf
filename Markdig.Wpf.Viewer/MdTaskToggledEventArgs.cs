namespace MarkdigWpfViewer;

using System.Windows;
using ViewModels;

/// <summary>Флажок задачи нажали: какую задачу и каким её хотят видеть; текст правит приложение.</summary>
public sealed class MdTaskToggledEventArgs : RoutedEventArgs
{
    public MdTaskToggledEventArgs(RoutedEvent routedEvent, object source, TaskVm task, bool isChecked)
        : base(routedEvent, source)
    {
        Task = task;
        IsChecked = isChecked;
    }

    /// <summary>Задача; её <see cref="ViewModels.NodeVm.Source"/> — место <c>[ ]</c> в исходнике.</summary>
    public TaskVm Task { get; }

    /// <summary>Каким флажок хотят видеть после нажатия.</summary>
    public bool IsChecked { get; }

    /// <inheritdoc />
    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget) =>
        ((EventHandler<MdTaskToggledEventArgs>)genericHandler)(genericTarget, this);
}