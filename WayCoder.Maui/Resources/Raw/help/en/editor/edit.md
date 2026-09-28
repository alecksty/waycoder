# Editing, find and replace

## Edit one line

**Tap that line** to start editing it (an input box floats over the line and the caret follows where your finger lands).

- Return = **split this line into two** (it does not "start a new line below")
- The new line **inherits the indentation**; if the line above ends with `{` `(` or `[`, it gets **one extra level**
- Typing `(` `[` `{` auto-inserts the closing half, leaving the caret in the middle
- Typing a closing bracket that is already there **steps over it** (you will not end up with `())`)

## Undo / redo

**↶ ↷** in the toolbar, or the same items in the ☰ menu.
Continuous typing is merged into a single step (it does not undo one character at a time).

## Selection

**Long-press** to select a word; keep holding and drag to extend the selection across lines.
An action bar then appears below: Copy / Delete / Select all / Paste / ✕.

## Find

☰ menu → **🔍 Find...**; type what you are looking for and it jumps to the first match **in the current file** (searching forward from the caret, and wrapping back to the top if nothing is found).

## Replace

☰ menu → **🔁 Replace...**: enter what to find → enter what to replace it with → choose:

- **Replace all**: changes everything at once, and **the whole thing is a single undo step**
- **Replace next only**: starts at the caret and changes the first occurrence

> Works only on **editable** files. Large files open read-only, and you will be told clearly that replacement is not possible.

## Assist input bar

The symbols that come up most in code (`(` `)` `[` `]` `{` `}` `<` `>` `"` `'` `;` `:` ...) sit on a small tappable pad at the bottom of the screen, so you do not have to keep switching keyboards.

- The **⌨** key toggles it; the whole bar can be **dragged to another position**
- Three tabs: **Symbols / Operators / Keywords**
- The keyword tab **switches with the language of the current file** (`.c` gives C keywords, `.py` gives Python ones)
- ⭐ Frequently used keys come first
- **⌫** is the app's own backspace — some keyboards' backspace never reaches the app, but this one always works
- After 10 seconds of no use it shrinks and fades; tap it to bring it back

## Line endings / encoding

Under Settings → Editor you can choose which encoding to save with (UTF-8 / with BOM / UTF-16 / local OEM) and which line ending to use.
The **default is "Keep as is"** — opening a file for a look and saving it again will not silently convert your GBK file to UTF-8.
