# Beresta.Markdown.Wpf.Editor v0.9.1

[![NuGet](https://img.shields.io/nuget/v/Beresta.Markdown.Wpf.Editor)](https://www.nuget.org/packages/Beresta.Markdown.Wpf.Editor)
[![Downloads](https://img.shields.io/nuget/dt/Beresta.Markdown.Wpf.Editor)](https://www.nuget.org/packages/Beresta.Markdown.Wpf.Editor)
[![Publish](https://img.shields.io/github/actions/workflow/status/porohkun/Beresta.Markdown.Wpf/nuget.yml?branch=main&label=publish)](https://github.com/porohkun/Beresta.Markdown.Wpf/actions/workflows/nuget.yml)
![.NET 7, 8, 9, 10](https://img.shields.io/badge/.NET-7%20%7C%208%20%7C%209%20%7C%2010-512BD4)
[![License](https://img.shields.io/github/license/porohkun/Beresta.Markdown.Wpf)](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/LICENSE)

[English](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/Beresta.Markdown.Wpf.Editor/README.md) | **Русский**

Редактор Markdown для WPF, который правит исходник прямо там, где он нарисован. `MarkdownEditor` — это
вьюер [Beresta.Markdown.Wpf](https://www.nuget.org/packages/Beresta.Markdown.Wpf) со всем, что он умеет, и
исходником в [AvalonEdit](https://github.com/icsharpcode/AvalonEdit): блоки под кареткой показывают свой
Markdown, а остальной документ остаётся нарисованным.

![Блок под кареткой показывает свой исходник](https://raw.githubusercontent.com/wiki/porohkun/Beresta.Markdown.Wpf/images/editor-blocks-ru.png)

## Что он умеет

- **Четыре режима.** `View` только показывает; в `Controls` флажки задач, сворачивание и карточка
  свойств пишут в исходник; `Blocks` показывает блоки под кареткой их исходником; `Full` ставит весь
  исходник на место отрисовки.
- **Исходник выглядит как отрисовка.** Правимый блок сохраняет высоту своего рисунка, и текст вокруг не
  сдвигается; заголовки сохраняют размер, код — подсветку, таблицы стоят столбцами.
- **Клавиши для Markdown.** Enter продолжает списки и цитаты, Tab сдвигает пункты, нумерованные списки
  перенумеровываются сами, Ctrl+B и Ctrl+I обрамляют жирным и курсивом, набранное ограждение кода
  закрывается само.
- **Поиск и замена** в исходнике, с пометками совпадений и в отрисовке.
- **Вставка с форматированием.** Ctrl+Shift+V переводит HTML из браузера или офисной программы в
  Markdown.
- **Front matter по типам.** Флажки, даты и списки карточки свойств записываются обратно тем видом,
  каким были записаны.

## Установка

```bash
dotnet add package Beresta.Markdown.Wpf.Editor
```

Нужен .NET 7 или новее (`net7.0-windows`) с WPF. Пакет приносит вьюер той же версии, AvalonEdit и
ReverseMarkdown для вставки HTML как Markdown.

Документация IntelliSense — на английском и русском: Visual Studio с русским интерфейсом показывает
русскую.

## Подключение тем

Влейте тему вьюера и после неё тему редактора в ресурсы приложения один раз.

`App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/Beresta.Markdown.Wpf;component/Themes/Generic.xaml" />
            <ResourceDictionary Source="pack://application:,,,/Beresta.Markdown.Wpf.Editor;component/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## Правка документа

Своей прокрутки у редактора нет: поставьте его в `ScrollViewer`, как вьюер.

`MainWindow.xaml`:

```xml
<Window x:Class="YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="https://github.com/porohkun/Beresta.Markdown.Wpf"
        Title="Markdown" Width="800" Height="600">
    <ScrollViewer>
        <md:MarkdownEditor x:Name="Editor"
                           Markdown="{Binding Text, Mode=TwoWay}"
                           EditMode="Blocks"
                           Margin="24,16" />
    </ScrollViewer>
</Window>
```

Правленый текст уходит в `Markdown` после каждой правки, и привязка сразу отдаёт его модели
представления; отрисовка догоняет его через секунду после последней правки.

## Документация

- [Редактор](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Editor-ru) — режимы, клавиши, отмена,
  front matter, вставка с форматированием и вид исходника.
- [Пометки и поиск](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Search-Highlighting-ru#поиск-и-замена-в-редакторе)
  — поиск и замена в исходнике.
- [Вики](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Home-ru) вьюера: редактор рисует документ
  так же, как вьюер, с теми же темами, расширениями и настройками.

## Лицензия

[MIT](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/LICENSE).
