namespace Beresta.Markdown.Wpf;

using Extensions;
using ViewModels;

/// <summary>
/// Подсветка блоков кода, запомненная по подсветчику, языку и коду: блок, который не менялся, заново
/// не подсвечивается и получает те же куски. Зовётся и из фона.
/// </summary>
internal sealed class CodeHighlights
{
    private readonly object _lock = new();
    private Dictionary<(IMdCodeHighlighter Highlighter, string Language, string Code), IReadOnlyList<InlineVm>> _known = [];
    private Dictionary<(IMdCodeHighlighter Highlighter, string Language, string Code), IReadOnlyList<InlineVm>> _used = [];

    /// <summary>Куски кода со стилями подсветки; подсвечивать нечего или подсветчик бросил — <c>null</c>.</summary>
    public IReadOnlyList<InlineVm>? Runs(string language, string code, IMdCodeHighlighter highlighter, ExtensionGuard guard)
    {
        var key = (highlighter, language, code);
        lock (_lock)
        {
            if (_used.TryGetValue(key, out var used))
            {
                return used;
            }

            if (_known.TryGetValue(key, out var known))
            {
                _used[key] = known;
                return known;
            }
        }

        if (guard.Call(null, () => highlighter.Highlight(language, code), null) is not { Count: > 0 } spans || code.Length == 0)
        {
            return null;
        }

        var runs = Split(code, spans);
        lock (_lock)
        {
            _used[key] = runs;
        }

        return runs;
    }

    /// <summary>Разбор лёг: помнится подсветка только его блоков.</summary>
    public void Retain()
    {
        lock (_lock)
        {
            _known = _used;
            _used = [];
        }
    }

    /// <summary>Код, разрезанный на куски по видам; вид позднего куска сильнее раннего.</summary>
    private static List<InlineVm> Split(string code, IReadOnlyList<MdCodeSpan> spans)
    {
        var looks = new string?[code.Length];
        foreach (var span in spans)
        {
            for (var i = Math.Max(0, span.Start); i < Math.Min(code.Length, span.Start + span.Length); i++)
            {
                looks[i] = span.Look;
            }
        }

        var runs = new List<InlineVm>();
        var start = 0;
        for (var i = 1; i <= code.Length; i++)
        {
            if (i < code.Length && looks[i] == looks[start])
            {
                continue;
            }

            var run = new RunVm(code[start..i]);
            runs.Add(looks[start] is { } look ? run with { StyleName = "Code." + look } : run);
            start = i;
        }

        return runs;
    }
}
