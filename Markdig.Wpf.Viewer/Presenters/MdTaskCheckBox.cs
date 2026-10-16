namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Controls;
using ViewModels;

/// <summary>
/// Флажок задачи: показывает исходник, сам не переключается, а нажатие отдаёт приложению через
/// <see cref="MarkdownViewer.TaskToggledEvent"/>. Доступен, пока у вьюера включено
/// <see cref="MarkdownViewer.CanToggleTasks"/>.
/// </summary>
public sealed class MdTaskCheckBox : CheckBox
{
    public static readonly DependencyProperty TaskProperty =
        DependencyProperty.Register(
            nameof(Task),
            typeof(TaskVm),
            typeof(MdTaskCheckBox),
            new(null, (d, e) => ((MdTaskCheckBox)d).IsChecked = (e.NewValue as TaskVm)?.IsChecked ?? false));

    private static readonly DependencyProperty CanToggleProperty =
        MarkdownViewer.CanToggleTasksProperty.AddOwner(
            typeof(MdTaskCheckBox),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, (d, _) => d.CoerceValue(IsEnabledProperty)));

    public MdTaskCheckBox()
    {
        CoerceValue(IsEnabledProperty);
    }

    public TaskVm? Task
    {
        get => (TaskVm?)GetValue(TaskProperty);
        set => SetValue(TaskProperty, value);
    }

    protected override bool IsEnabledCore => base.IsEnabledCore && (bool)GetValue(CanToggleProperty);

    protected override void OnToggle()
    {
        if (Task is { } task)
        {
            RaiseEvent(new MdTaskToggledEventArgs(MarkdownViewer.TaskToggledEvent, this, task, !task.IsChecked));
        }
    }
}