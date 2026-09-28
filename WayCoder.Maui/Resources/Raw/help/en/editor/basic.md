# Basics

## Opening a file

On the **Files** page, tap a file name → choose **Open**.

## Read-only by default

Files open in **read-only** mode (the status bar says "Read-only") — this stops a stray tap from wrecking your code.

To edit, tap **🔒** in the toolbar at the top right (or use **✎ Toggle edit/read-only** in the ☰ menu).
Tap it again to go back to read-only.

## Saving

**💾** in the toolbar, or **Save** in the ☰ menu.
Saving is **atomic** (a temp file is written first, then swapped in), so losing power halfway never leaves you with half a file.

> One exception: assembly and bytecode files such as `.vml` / `.vmb` always open read-only.
> They are build artifacts; to change them, change the source.

## Font size and gestures

- **Pinch with two fingers**: scale the font size
- Scaling is **continuous** (it does not jump in fixed steps)
- To go back to the default size: ☰ menu → **↩ Reset font**

## Fullscreen

Tap **⛶** in the top right to go fullscreen (other UI is hidden); tap again to leave.

## Horizontal scrolling

Long lines **do not wrap**. Swipe from left to right to see the rest (there is a horizontal scrollbar at the bottom).
The line-number gutter stays pinned to the left and does not scroll with the text.
