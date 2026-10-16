[English](Scrolling-and-Virtualization) | **Русский**

Своих полос прокрутки у `MarkdownViewer` нет. Он живёт в прокрутке, которая уже есть у приложения, и
создаёт контролы только для блоков, видимых сквозь все прокрутки вокруг него. Остальные блоки
существуют лёгкими моделями: выделение, копирование, поиск и оглавление всё равно работают со всем
документом.

## В ScrollViewer

Обычный случай: один документ занимает панель.

**Окно или пользовательский контрол**:

```xml
<ScrollViewer>
    <mdv:MarkdownViewer Markdown="{Binding Text}" Margin="24,16" />
</ScrollViewer>
```

## Частью страницы

Вьюер может стоять на странице рядом с другим содержимым, с одной полосой прокрутки на всё. Создаётся
только видимая часть документа, даже когда вьюер намного выше окна.

**Окно или пользовательский контрол**:

```xml
<ScrollViewer>
    <StackPanel Margin="24,16">
        <TextBlock Text="{Binding Title}" FontSize="24" />
        <mdv:MarkdownViewer Markdown="{Binding Body}" />
        <Button Content="Reply" HorizontalAlignment="Left" />
    </StackPanel>
</ScrollViewer>
```

## В виртуализированном списке

Чат или лента: вьюер на каждое сообщение в виртуализированном списке. Список создаёт только видимые
сообщения, а длинное сообщение — только свои видимые блоки.

**Окно или пользовательский контрол**:

```xml
<ItemsControl ItemsSource="{Binding Messages}"
              VirtualizingPanel.IsVirtualizing="True"
              VirtualizingPanel.ScrollUnit="Pixel"
              ScrollViewer.CanContentScroll="True">
    <ItemsControl.Template>
        <ControlTemplate TargetType="ItemsControl">
            <ScrollViewer CanContentScroll="True">
                <ItemsPresenter />
            </ScrollViewer>
        </ControlTemplate>
    </ItemsControl.Template>
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel />
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <mdv:MarkdownViewer Markdown="{Binding Text}" Margin="8" />
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

`VirtualizingPanel.ScrollUnit="Pixel"` прокручивает длинные сообщения плавно, а не прыжками по целым
элементам. Без него не работает и прокрутка к строке исходника, куску текста или якорю: вьюер двигает
прокрутку в пикселях.

## Без прокрутки

Без прокрутки вокруг вьюер создаёт все блоки и занимает высоту всего документа. Для коротких текстов
это нормально: подсказка, карточка, диалог.

## Вложенная прокрутка

Широкие таблицы и блоки кода прокручиваются вбок сами, но колесо мыши отдают странице: документ
продолжает прокручиваться, пока указатель над ними, а Shift + колесо двигает их вбок. Своему виджету
со своим `ScrollViewer` то же поведение даёт одно вложенное свойство.

**Шаблон своего виджета** (см. [Расширения](Extensions-ru)):

```xml
<ScrollViewer HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Disabled"
              b:NestedScroll.PassWheelToParent="True">
    <TextBlock Text="{Binding Wide}" />
</ScrollViewer>
```

## Новый текст не сдвигает видимое

Новое значение `Markdown` ложится правками: неизменившиеся блоки сохраняют контролы и измеренные
высоты, а видимое не уезжает, когда текст меняется выше него, — см.
[Живой предпросмотр](Live-Preview-ru).
