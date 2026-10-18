[[_TOC_]]

Above is the GitLab table of contents, `[[_TOC_]]`: it is built from the headings of the document, and a click
on an item scrolls to its section.

## What is in

- fixes #42 :rocket: — the new badge color is `#FC6D26`;
- closes #57 — tables in the dark theme :bug:;
- an issue link leads to GitLab by the template of the showcase, and a color code is shown with a swatch.

## How to check

1. Build the showcase and open the Sets tab.
2. Switch the theme — the swatches of `#FC6D26` and `#1F75CB` keep their colors.
3. Click issue #42 — its page opens.

```bash
dotnet run --project Beresta.Markdown.Wpf.Showcase
```

## Staging

Built on staging and checked in the light and the dark theme :white_check_mark:. Before the rollout
we looked at the badge on `#FAFAFA` and on the dark `#1F1F1F`.

Review notes:

- the caption under the badge was off by a pixel — fixed;
- in a narrow window the table of contents wraps by words — as intended;
- issue links without a template stay text.

## Production

Rolled out after the review :white_check_mark: — #42 and #57 are closed.

The next release goes out when we close #61 :eyes:.
