# Compile, run, and diagnostics

## Run in place

That toolbar cell **changes shape with the file type**:

| File | The cell says | Tapping it |
|---|---|---|
| Compilable source (`.c` `.py` ...) | **▶ Run** | Compile + link + run; output appears in the bottom panel |
| Markdown (`.md`) | **👁 Preview** | Renders tables and headings |

If the program needs **keyboard input**, a send box appears in the bottom panel — type a line and press Return to feed it in.

## Stopping

Tap **■** on the right of the bottom panel. If a program will not stop, press Back to leave the window.

## Errors are drawn on the code

When compilation fails, the error is **marked directly on the offending line**:

- A **bubble** appears at the end of the line with the error message (multiple errors are staggered so they do not overlap)
- A **squiggle** is drawn at the error position (red = error / orange = warning / green = hint)
- The bottom panel switches to the **Problems** tab, listing every issue; tap one to jump to that line

Bubbles are **part of the code**: they scroll with it and scale with it, and their position depends only on code coordinates.

> ⚠ As soon as **you touch the code**, the previous round of errors is cleared — those line numbers have
> probably shifted, and leaving them floating over the code would only show you false information.

## Status bar

One line at the bottom shows: `encoding · read-only/edit · lines · size · caret position · font size`.
At a glance you can see whether you can edit right now, and where the caret is.
