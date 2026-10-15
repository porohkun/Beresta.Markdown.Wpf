namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Controls;
using ViewModels;

/// <summary>Блок кода: рамка с прокруткой вбок вокруг текста кода.</summary>
public class CodeBlockPresenter : Control
{
    /// <summary>Код одним куском текста — тот же список, что у вьюмодели, ради выделения.</summary>
    public static readonly DependencyProperty RunsProperty =
        DependencyProperty.Register(
            nameof(Runs),
            typeof(IReadOnlyList<InlineVm>),
            typeof(CodeBlockPresenter));

    public static readonly DependencyProperty CodeProperty =
        DependencyProperty.Register(
            nameof(Code),
            typeof(string),
            typeof(CodeBlockPresenter),
            new(null, OnCodeChanged));

    private static readonly DependencyPropertyKey LineNumbersPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(LineNumbers),
            typeof(string),
            typeof(CodeBlockPresenter),
            new(string.Empty));

    /// <summary>Номера строк кода столбцом, с единицы.</summary>
    public static readonly DependencyProperty LineNumbersProperty = LineNumbersPropertyKey.DependencyProperty;

    public static readonly DependencyProperty SyntaxProperty =
        DependencyProperty.Register(
            nameof(Syntax),
            typeof(string),
            typeof(CodeBlockPresenter));

    public static readonly DependencyProperty InfoStringProperty =
        DependencyProperty.Register(
            nameof(InfoString),
            typeof(string),
            typeof(CodeBlockPresenter));

    public IReadOnlyList<InlineVm>? Runs
    {
        get => (IReadOnlyList<InlineVm>?)GetValue(RunsProperty);
        set => SetValue(RunsProperty, value);
    }

    public string? Code
    {
        get => (string?)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public string LineNumbers => (string)GetValue(LineNumbersProperty);

    public string? Syntax
    {
        get => (string?)GetValue(SyntaxProperty);
        set => SetValue(SyntaxProperty, value);
    }

    public string? InfoString
    {
        get => (string?)GetValue(InfoStringProperty);
        set => SetValue(InfoStringProperty, value);
    }

    private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var lines = ((string?)e.NewValue ?? string.Empty).Split('\n').Length;
        d.SetValue(LineNumbersPropertyKey, string.Join("\n", Enumerable.Range(1, lines)));
    }
}