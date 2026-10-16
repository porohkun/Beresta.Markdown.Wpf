namespace MarkdigWpfViewer.Presenters;

using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Selection;
using ViewModels;

/// <summary>Блок кода: рамка с шапкой языка, прокруткой вбок вокруг текста кода и кнопкой копирования.</summary>
[TemplatePart(Name = HeaderPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = CodePart, Type = typeof(MdTextBlock))]
[TemplatePart(Name = CopyPart, Type = typeof(ButtonBase))]
public class CodeBlockPresenter : Control
{
    private static readonly DependencyPropertyKey IsCopiedPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsCopied),
            typeof(bool),
            typeof(CodeBlockPresenter),
            new(false));

    /// <summary>Код только что скопирован кнопкой — на миг после нажатия.</summary>
    public static readonly DependencyProperty IsCopiedProperty = IsCopiedPropertyKey.DependencyProperty;

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
    private const string CopyPart = "PART_Copy";

    private static readonly TimeSpan CopiedFor = TimeSpan.FromSeconds(1.5);

    private ButtonBase? _copy;
    private DispatcherTimer? _copied;

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

    public bool IsCopied => (bool)GetValue(IsCopiedProperty);

    public override void OnApplyTemplate()
    {
        if (_copy is not null)
        {
            _copy.Click -= OnCopyClick;
        }

        base.OnApplyTemplate();

        _copy = GetTemplateChild(CopyPart) as ButtonBase;
        if (_copy is not null)
        {
            _copy.Click += OnCopyClick;
        }
    }

    /// <summary>Кладёт код в буфер обмена: текстом, блоком кода markdown и HTML; через <see cref="DataObject.CopyingEvent"/>.</summary>
    public void Copy()
    {
        if (CreateCopyData() is not { } data)
        {
            return;
        }

        MdClipboard.Put(data);
        SetValue(IsCopiedPropertyKey, true);
        _copied ??= new(CopiedFor, DispatcherPriority.Normal, (_, _) => EndCopied(), Dispatcher);
        _copied.Stop();
        _copied.Start();
    }

    /// <summary>Код для буфера после <see cref="DataObject.CopyingEvent"/>; <c>null</c> — кода нет или приложение отменило.</summary>
    internal DataObject? CreateCopyData()
    {
        if (Code is not { } code)
        {
            return null;
        }

        var fence = new string('`', Math.Max(3, MdMarkdownWriter.LongestRun(code, '`') + 1));
        var markdown = $"{fence}{InfoString}\n{code}\n{fence}";
        var language = Syntax is { Length: > 0 } syntax
            ? $" class=\"language-{WebUtility.HtmlEncode(syntax)}\""
            : string.Empty;
        var html = $"<pre><code{language}>{WebUtility.HtmlEncode(code)}</code></pre>";

        var data = MdClipboard.Data(code, markdown, html);
        return MdClipboard.Approve(this, data) ? data : null;
    }

    private void OnCopyClick(object sender, RoutedEventArgs e) => Copy();

    private void EndCopied()
    {
        _copied?.Stop();
        SetValue(IsCopiedPropertyKey, false);
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
