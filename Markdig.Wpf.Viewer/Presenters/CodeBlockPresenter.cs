namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Controls;
using ViewModels;

/// <summary>Блок кода: рамка с шапкой языка и прокруткой вбок вокруг текста кода.</summary>
[TemplatePart(Name = HeaderPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = CodePart, Type = typeof(MdTextBlock))]
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

    /// <summary>Код в ограждении: у блока есть шапка — экранная строка открывающего ограждения.</summary>
    public static readonly DependencyProperty IsFencedProperty =
        DependencyProperty.Register(
            nameof(IsFenced),
            typeof(bool),
            typeof(CodeBlockPresenter),
            new(false));

    private const string HeaderPart = "PART_Header";
    private const string CodePart = "PART_Code";

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

    public bool IsFenced
    {
        get => (bool)GetValue(IsFencedProperty);
        set => SetValue(IsFencedProperty, value);
    }

    /// <summary>Строки исходника блока — шапка и строки кода — и их полосы в своих координатах.</summary>
    internal IEnumerable<(int Line, Rect Row)> SourceRows()
    {
        if (DataContext is not CodeBlockVm code)
        {
            yield break;
        }

        if (GetTemplateChild(HeaderPart) is FrameworkElement { IsVisible: true, ActualHeight: > 0 } header)
        {
            yield return (code.Source.StartLine, header.TransformToAncestor(this).TransformBounds(new(header.RenderSize)));
        }

        if (GetTemplateChild(CodePart) is FrameworkElement { ActualHeight: > 0 } text)
        {
            var lines = code.Code.Split('\n').Length;
            var top = text.TranslatePoint(default, this).Y;
            var height = text.ActualHeight / lines;

            for (var i = 0; i < lines; i++)
            {
                yield return (code.CodeFirstLine + i, new(0, top + i * height, 0, height));
            }
        }
    }

    private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var lines = ((string?)e.NewValue ?? string.Empty).Split('\n').Length;
        d.SetValue(LineNumbersPropertyKey, string.Join("\n", Enumerable.Range(1, lines)));
    }
}
