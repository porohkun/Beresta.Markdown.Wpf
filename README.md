# Markdig.Wpf.Viewer v0.4.0

Markdown viewer for WPF based on Markdig.

## Ссылки

По умолчанию нажатая ссылка открывается системным обработчиком, как и раньше. Но сначала вьюер
спрашивает приложение: поднимает всплывающее `MarkdownLink.Navigate`, и обработанное событие
внешнее открытие отменяет.

Это и есть точка для своих ссылок вида `[подпись](своя_схема:идентификатор)`. Подключается она
двумя способами, оба — на вьюер или любого его предка:

```xml
<!-- обработчиком -->
<mdv:MarkdownViewer Markdown="{Binding Text}"
                    mdv:MarkdownLink.Navigate="OnMarkdownLink" />

<!-- командой; параметр — Href -->
<mdv:MarkdownViewer Markdown="{Binding Text}"
                    mdv:MarkdownLink.Command="{Binding OpenLinkCommand}" />
```

```csharp
private void OnMarkdownLink(object sender, MarkdownLinkEventArgs e)
{
    if (!MyId.TryParse(e.Href, out var id))
        return;         // не наша ссылка — пусть открывается наружу

    Open(id);
    e.Handled = true;   // наша — наружу не отдаём
}
```

Цель ссылки приходит дважды: `Href` — строка ровно из markdown, `Uri` — её разбор. Для своих
схем нужен `Href`: схема с подчёркиванием (`codex_page:...`) по RFC 3986 незаконна, и такой
адрес разбирается в относительный `Uri`. Команда, у которой `CanExecute(href)` вернул `false`,
событие не гасит — ссылка уходит наружу, как если бы команды не было вовсе.
