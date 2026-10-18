# Extensions

Three extensions of the showcase are connected here, each on its own: a note, a mention and a chip. A note
can be collapsed with a click on its title — it remembers that even after leaving the visible area. A mention
is copied as `@name`.

:::note Note
Inside a note is ordinary markup: **bold**, `code`, [a link](https://example.com).

- list items
- and a mention @ivan right in an item
:::

Mentions live right in the text: write @ivan or @maria, and they turn into pills. A click on a
mention leads to the profile the same way as a link in the text does.

A chip is styled text, not a widget: ::done:: and ::a chip with **bold**:: are selected in pieces,
found by search and copied as markdown back into `::…::`. The look of a chip is a showcase style named after
its styling.

::::note Nesting
A note can contain other notes.

:::note Inner
A nested note with **emphasis**.
:::
::::

Everything the extensions did not recognize is parsed as usual: a `:::warning` without an extension of its
own shows its content.

:::warning
An unknown container — the content is not lost.
:::
