namespace Beresta.Markdown.Wpf.Extensions;

/// <summary>
/// Вызов кода расширения или его виджета: исключение не уходит дальше, а отдаётся получателю сбоев
/// вместе с расширением, и вызов отвечает запасным значением.
/// </summary>
internal sealed class ExtensionGuard
{
    private readonly Action<Exception, MdExtension?> _failed;
    private readonly Func<object, MdExtension?> _ownerOf;

    /// <summary>
    /// Обёртка с получателем сбоев <paramref name="failed"/>; <paramref name="ownerOf"/> отвечает,
    /// какое расширение создало виджет.
    /// </summary>
    public ExtensionGuard(Action<Exception, MdExtension?> failed, Func<object, MdExtension?>? ownerOf = null)
    {
        _failed = failed;
        _ownerOf = ownerOf ?? (_ => null);
    }

    /// <summary>Обёртка, которая сбои не отдаёт никому.</summary>
    public static ExtensionGuard Silent { get; } = new((_, _) => { });

    /// <summary>Ответ кода расширения; бросил — <paramref name="fallback"/>.</summary>
    public T Call<T>(MdExtension? extension, Func<T> call, T fallback)
    {
        try
        {
            return call();
        }
        catch (Exception exception)
        {
            _failed(exception, extension);
            return fallback;
        }
    }

    /// <summary>Выполняет код расширения; бросил — <c>false</c>.</summary>
    public bool Call(MdExtension? extension, Action call)
        => Call(
            extension,
            () =>
            {
                call();
                return true;
            },
            false);

    /// <summary>Ответ кода виджета; бросил — <paramref name="fallback"/>, сбой — на расширение, создавшее виджет.</summary>
    public T CallWidget<T>(object widget, Func<T> call, T fallback) => Call(_ownerOf(widget), call, fallback);

    /// <summary>Выполняет код виджета; бросил — <c>false</c>, сбой — на расширение, создавшее виджет.</summary>
    public bool CallWidget(object widget, Action call) => Call(_ownerOf(widget), call);
}
