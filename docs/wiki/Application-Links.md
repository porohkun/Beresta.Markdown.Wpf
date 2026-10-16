**English** | [Русский](Application-Links-ru)

A link may lead into your application instead of the web: `[the plan](note:plan-2026)` opens a note,
`[Anna](user:anna)` opens a profile. Before doing anything with a click, the viewer asks your
application, and the application may also replace what happens with the links nobody handled. The
whole order of a click is in [Links and Anchors](Links-and-Anchors#what-a-click-does).

## The Navigate event

`MarkdownLink.Navigate` is a bubbling routed event: a handler on the viewer or on any of its ancestors
gets every click on a link, a linked image, a footnote reference or `↩` — and on a link of an
[extension widget](Extensions#links-from-a-widget). A handled event stops there.

**`MainWindow.xaml`**:

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}" mdv:MarkdownLink.Navigate="OnMarkdownLink" />
```

**`MainWindow.xaml.cs`**, in the window class:

```csharp
private void OnMarkdownLink(object sender, MarkdownLinkEventArgs e)
{
    if (!e.Href.StartsWith("note:", StringComparison.Ordinal))
    {
        return;
    }

    MessageBox.Show($"Open the note {e.Href["note:".Length..]}");
    e.Handled = true;
}
```

A link that is not yours stays unhandled and goes on as usual: anchors scroll, web links open.

The target comes twice: `Href` is the string exactly as written in the Markdown, `Uri` is its parse.
For your own schemes use `Href`: a scheme with an underscore, like `my_app:...`, is not a valid URI
scheme, and such an address parses into a relative `Uri`. Anchors come as `#id`, footnotes as `#fn:1`
and `#fnref:1`, so the event can take over in-document navigation too.

## A command instead of a handler

With MVVM, attach a command: it gets `Href` as its parameter. A command whose `CanExecute(href)` returns
`false` leaves the event unhandled, exactly like a handler that returns.

**`MainWindow.xaml`**:

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}" mdv:MarkdownLink.Command="{Binding OpenLinkCommand}" />
```

**Your view model**, with any `ICommand` implementation — here `RelayCommand<string>` from
CommunityToolkit.Mvvm:

```csharp
public MainViewModel()
{
    OpenLinkCommand = new RelayCommand<string>(
        href => OpenNote(href!["note:".Length..]),
        href => href?.StartsWith("note:", StringComparison.Ordinal) == true);
}

public ICommand OpenLinkCommand { get; }
```

`OpenNote` stands for your own code.

## Your own link opener

The links nobody handled go to the link opener — by default `MdSystemLinkOpener`, which opens web and
mail addresses with the system handler and ignores the rest. Replace it to change that for every link
at once: ask before leaving the application, open files, log clicks. The opener is the inherited
attached property `MarkdownLink.Opener`: set it on a viewer, a window or in a style.

**`ConfirmingLinkOpener.cs`**:

```csharp
using System.Windows;
using MarkdigWpfViewer;

public sealed class ConfirmingLinkOpener : IMdLinkOpener
{
    public void Open(string href, Uri? uri)
    {
        if (MessageBox.Show($"Open {href}?", "Link", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            MdSystemLinkOpener.Instance.Open(href, uri);
        }
    }
}
```

**`MainWindow.xaml.cs`**, in the constructor after `InitializeComponent()`; it covers every viewer of
the window:

```csharp
MarkdownLink.SetOpener(this, new ConfirmingLinkOpener());
```

`mdv:MarkdownLink.Opener="{x:Null}"` on a viewer means no link of it leaves the application; the
`Navigate` event and anchors keep working.
