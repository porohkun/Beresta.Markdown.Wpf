namespace Beresta.Markdown.Wpf.Kit;

/// <summary>Что знает приложение о целях связей готовых расширений: есть ли страница, как зовут участника.</summary>
public interface IMdLinkResolver
{
    /// <summary>Есть ли вики-страница с этим именем; по умолчанию — есть.</summary>
    bool PageExists(string page) => true;

    /// <summary>Отображаемое имя участника по имени из разметки; <c>null</c> — как написано.</summary>
    string? DisplayName(string user) => null;
}
