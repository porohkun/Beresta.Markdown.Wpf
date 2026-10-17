namespace MarkdigWpfViewer.Presenters;

using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Threading;
using Scrolling;
using Selection;
using ViewModels;

/// <summary>Блок кода: рамка с шапкой языка, прокруткой вбок вокруг текста кода и кнопкой копирования.</summary>
[TemplatePart(Name = HeaderPart, Type = typeof(FrameworkElement))]
[TemplatePart(Name = CodePart, Type = typeof(MdTextBlock))]
[TemplatePart(Name = CopyPart, Type = typeof(ButtonBase))]
[TemplatePart(Name = NumbersPart, Type = typeof(TextBlock))]
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
    private const string NumbersPart = "LineNumbers";

    private static readonly TimeSpan CopiedFor = TimeSpan.FromSeconds(1.5);

    private static readonly DependencyProperty GapsProperty =
        GapLayout.LayoutProperty.AddOwner(
            typeof(CodeBlockPresenter),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, (d, _) => ((CodeBlockPresenter)d).UpdateNumbers()));

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

        UpdateNumbers();
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

        if (GetTemplateChild(CodePart) is not MdTextBlock { ActualHeight: > 0 } text)
        {
            yield break;
        }

        var toPresenter = text.TransformToAncestor(this);
        var offset = 0;
        var lines = code.Code.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (text.RowAt(offset) is { } row)
            {
                yield return (code.CodeFirstLine + i, toPresenter.TransformBounds(row));
            }

            offset += lines[i].Length + 1;
        }
    }

    private static void OnCodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var lines = ((string?)e.NewValue ?? string.Empty).Split('\n').Length;
        d.SetValue(LineNumbersPropertyKey, string.Join("\n", Enumerable.Range(1, lines)));
        ((CodeBlockPresenter)d).UpdateNumbers();
    }

    /// <summary>Номера строк столбцом, а напротив пустых мест в коде — такие же пустые места.</summary>
    private void UpdateNumbers()
    {
        if (GetTemplateChild(NumbersPart) is not TextBlock numbers)
        {
            return;
        }

        var gaps = DataContext is CodeBlockVm code && GetValue(GapsProperty) is GapLayout layout ? layout.InCode(code) : [];
        var count = (Code ?? string.Empty).Split('\n').Length;
        var column = new StringBuilder();

        // BYDESIGN: номера — всегда строчные элементы, без Text: у измеренного TextBlock переход от Text к
        // строчным элементам тут же запускает раскладку, а сюда приходят и посреди обхода дерева.
        numbers.Inlines.Clear();

        for (var i = 0; i < count; i++)
        {
            foreach (var (_, height) in gaps.Where(gap => gap.Index == i))
            {
                if (column.Length > 0)
                {
                    numbers.Inlines.Add(new Run(column.ToString()));
                    column.Clear();
                }

                numbers.Inlines.Add(new InlineUIContainer(new Border { Width = 1, Height = height }));
                numbers.Inlines.Add(new LineBreak());
            }

            column.Append(i + 1);
            if (i < count - 1)
            {
                column.Append('\n');
            }
        }

        numbers.Inlines.Add(new Run(column.ToString()));
        foreach (var (_, height) in gaps.Where(gap => gap.Index >= count))
        {
            numbers.Inlines.Add(new LineBreak());
            numbers.Inlines.Add(new InlineUIContainer(new Border { Width = 1, Height = height }));
        }
    }

    private void OnCopyClick(object sender, RoutedEventArgs e) => Copy();

    private void EndCopied()
    {
        _copied?.Stop();
        SetValue(IsCopiedPropertyKey, false);
    }
}