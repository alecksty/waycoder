# Text

Writing text, fonts, anchors.

> For the full list see "[UI development](help:vml/ui)". Each call below comes with a
> one-line usage note and an example; signatures come from `Lib/c/waycoder_ui.h` (the authoritative source).

### `ui_set_font(int size, int style, int color, int anchor)`
Sets the font once, and every later `ui_text_cur` uses it (so you do not have to pass the same four arguments again and again).
```c
ui_set_font(18, VML_FONT_BOLD, 0xFFFFFFFF, VML_ANCHOR_CENTER);
ui_text_cur(180, 40, "Press a direction key to exit");
```
### `ui_set_valign(int valign)`
Sets the **default** vertical alignment (it applies to every text drawn afterwards).
```c
ui_set_valign(VML_VALIGN_MIDDLE);
```
### `ui_text(int x, int y, char* s, int color, int size, int anchor)`
Writes one line of text at (x,y). `size` is the font size; `anchor` decides which side of the text (x,y) refers to (`VML_ANCHOR_LEFT` / `CENTER` / `RIGHT`).
```c
ui_text(180, 40, "Score: 120", 0xFFFFFFFF, 20, VML_ANCHOR_CENTER);
```
### `ui_text_cur(int x, int y, char* s)`
Draws text with the font set by `ui_set_font`.
```c
ui_text_cur(180, 300, "Game over");
```
### `ui_text_styled(int x, int y, char* s, int color, int size, int anchor, int style)`
The same, plus a style (bold / italic / underline).
```c
ui_text_styled(10, 10, "Title", 0xFFFFFFFF, 22, VML_ANCHOR_LEFT, VML_FONT_BOLD);
```
### `ui_text_v(int x, int y, char* s, int color, int size, int anchor, int valign, int style)`
Text with **vertical alignment** (`VML_VALIGN_*`) - for `ui_text` the y is the baseline, while this one can align to top / middle / bottom.
```c
ui_text_v(200, 100, "Centered", 0xFFFFFFFF, 20, VML_ANCHOR_CENTER, VML_VALIGN_MIDDLE, 0);
```
